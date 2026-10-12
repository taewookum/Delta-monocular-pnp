from __future__ import annotations

from pathlib import Path

import tensorrt as trt
import torch


TRT_TO_TORCH = {
    trt.float32: torch.float32,
    trt.float16: torch.float16,
    trt.int32: torch.int32,
    trt.int8: torch.int8,
    trt.bool: torch.bool,
}


class TensorRTEngine:
    """TensorRT 8.6 runner backed by PyTorch CUDA allocations."""

    def __init__(self, engine_path: Path, device: str = "cuda:0") -> None:
        if not torch.cuda.is_available():
            raise RuntimeError("CUDA is unavailable")
        self.path = engine_path.resolve()
        self.device = torch.device(device)
        self.logger = trt.Logger(trt.Logger.WARNING)
        trt.init_libnvinfer_plugins(self.logger, "")
        self.runtime = trt.Runtime(self.logger)
        self.engine = self.runtime.deserialize_cuda_engine(self.path.read_bytes())
        if self.engine is None:
            raise RuntimeError(f"failed to deserialize TensorRT engine: {self.path}")
        self.context = self.engine.create_execution_context()
        if self.context is None:
            raise RuntimeError("failed to create TensorRT execution context")
        self.input_names = []
        self.output_names = []
        for index in range(self.engine.num_io_tensors):
            name = self.engine.get_tensor_name(index)
            mode = self.engine.get_tensor_mode(name)
            if mode == trt.TensorIOMode.INPUT:
                self.input_names.append(name)
            else:
                self.output_names.append(name)
        if not self.input_names or not self.output_names:
            raise RuntimeError("engine has no input or output tensors")
        self.last_input_data_ptr: int | None = None

    def _dtype(self, name: str) -> torch.dtype:
        trt_dtype = self.engine.get_tensor_dtype(name)
        try:
            return TRT_TO_TORCH[trt_dtype]
        except KeyError as error:
            raise TypeError(
                f"unsupported TensorRT dtype for {name}: {trt_dtype}"
            ) from error

    def _prepare_io(
        self, image: torch.Tensor
    ) -> tuple[torch.Tensor, dict[str, torch.Tensor], torch.cuda.Stream]:
        """Bind one input and allocate outputs before the measured GPU region.

        Input conversion/copy and output allocation intentionally happen before
        the CUDA start event used by :meth:`infer_timed`.  This keeps the
        reported value scoped to TensorRT ``execute_async_v3`` only.
        """
        if len(self.input_names) != 1:
            raise RuntimeError("this runner currently expects one engine input")
        input_name = self.input_names[0]
        image = image.to(
            device=self.device, dtype=self._dtype(input_name), non_blocking=True
        ).contiguous()
        engine_shape = tuple(self.engine.get_tensor_shape(input_name))
        if -1 in engine_shape:
            if not self.context.set_input_shape(input_name, tuple(image.shape)):
                raise RuntimeError(f"failed to set input shape for {input_name}")
        elif tuple(image.shape) != engine_shape:
            raise ValueError(
                f"input shape mismatch: engine={engine_shape}, tensor={tuple(image.shape)}"
            )
        if not self.context.set_tensor_address(input_name, image.data_ptr()):
            raise RuntimeError(f"failed to bind input tensor {input_name}")
        self.last_input_data_ptr = int(image.data_ptr())

        outputs: dict[str, torch.Tensor] = {}
        for name in self.output_names:
            shape = tuple(self.context.get_tensor_shape(name))
            if any(dimension < 0 for dimension in shape):
                raise RuntimeError(
                    f"unresolved dynamic output shape for {name}: {shape}"
                )
            tensor = torch.empty(shape, dtype=self._dtype(name), device=self.device)
            if not self.context.set_tensor_address(name, tensor.data_ptr()):
                raise RuntimeError(f"failed to bind output tensor {name}")
            outputs[name] = tensor

        stream = torch.cuda.current_stream(self.device)
        return image, outputs, stream

    def _execute(self, stream: torch.cuda.Stream) -> None:
        if not self.context.execute_async_v3(stream.cuda_stream):
            raise RuntimeError("TensorRT execute_async_v3 returned false")

    def __call__(self, image: torch.Tensor) -> dict[str, torch.Tensor]:
        bound_input, outputs, stream = self._prepare_io(image)
        self._execute(stream)
        # Keep this small deployment API synchronous so converted input buffers
        # cannot be released while TensorRT still reads them.
        stream.synchronize()
        del bound_input
        return outputs

    def infer_timed(self, image: torch.Tensor) -> tuple[dict[str, torch.Tensor], float]:
        """Run one synchronous inference timed only with CUDA events.

        Engine deserialization, input conversion/copy, output allocation and
        warm-up are outside the measured interval.  The returned milliseconds
        therefore represent the GPU TensorRT execution region required by the
        depth-evaluation protocol.
        """
        bound_input, outputs, stream = self._prepare_io(image)
        started = torch.cuda.Event(enable_timing=True)
        finished = torch.cuda.Event(enable_timing=True)
        started.record(stream)
        self._execute(stream)
        finished.record(stream)
        finished.synchronize()
        inference_ms = float(started.elapsed_time(finished))
        del bound_input
        return outputs, inference_ms
