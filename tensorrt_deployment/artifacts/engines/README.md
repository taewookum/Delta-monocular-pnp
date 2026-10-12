# TensorRT engines

The webcam program expects the engines at these paths:

```text
tensorrt_deployment/artifacts/engines/
  adabins_ft_eval_rgb480x640_fp16.engine
  yolov8s_fp16.engine
```

The files are excluded from Git and are extracted from the external TensorRT
archive by:

```bash
python scripts/download_assets.py --kind models
```

The engines were built for the Jetson environment documented in the root
README and may not load with another GPU architecture, CUDA version, or
TensorRT version. The deployed AdaBins engine uses the earlier epoch-24
checkpoint rather than the released 20-epoch evaluation checkpoint.
