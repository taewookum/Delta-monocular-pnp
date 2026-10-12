# On-device monocular picking

This repository contains the code used to run monocular Depth and Detection
inference on an NVIDIA Jetson, exchange results with a Windows robot-control
GUI, and operate the GPIO/UART interface used in the experiments.

## Contents

- `test_webcam_adabins_yolo_tensorRT_fp16.py`: webcam and TensorRT inference
- `ard_test_GPIO.py`: Jetson GPIO and UART interface
- `tensorrt_deployment/ral_trt/`: TensorRT runtime support
- `Robot_GUI/`: Windows Forms robot-control GUI
- `weights/README.md`: fine-tuned experiment weight download links
- `scripts/download_assets.py`: dataset and engine downloader
- `data/manifest.json`: dataset download information
- `artifacts/manifest.json`: TensorRT engine download information

Training code and ONNX export scripts are not included in this runtime
package. Large datasets, fine-tuned weights, and TensorRT engines are hosted
separately and are excluded from Git.

## Datasets

| Dataset | Train | Validation | Test | Total |
| --- | ---: | ---: | ---: | ---: |
| Depth | 30,641 pairs | 864 pairs | 1,097 pairs | 32,602 pairs |
| Detection | 3,381 pairs | 199 pairs | 402 pairs | 3,982 pairs |

- [Depth dataset](https://drive.google.com/file/d/1HvFzt6cVB4kj__SZr1kLksfSv2X6MWPh/view?usp=sharing)
- [Detection dataset](https://drive.google.com/file/d/1-wUX-Eqa-RUqGxmrVZ9NkU0q-ULWhb7A/view?usp=sharing)

The 1,097-pair Depth test set was captured with a separate sensor (D435i) and
randomly sampled; it is disjoint from the training and validation splits.

Evaluation scripts are not included in this runtime repository. Table I
dataset metrics (delta1, AbsRel, and RMSE) were calculated from paired depth
predictions and references, while MAE, signed Bias, and the percentages within
5 and 10 mm were calculated from physical-touch errors. Table II reports
Precision, Recall, F1, mAP50, and mAP50-95 from the Ultralytics validation
pipeline, with Precision, Recall, and F1 evaluated at each model's fixed
confidence threshold.

## Experiment weights

The fine-tuned weights used in the experiments are available below.

- [AdaBins Depth weight](https://drive.google.com/file/d/17MVEp70SIyEU8NeZnlTqbPILmsorHn2j/view?usp=sharing)
- [YOLOv8s Detection weight](https://drive.google.com/file/d/1qi8FhNN5fJC0HVqwJ54J-OF8DboPPdXR/view?usp=sharing)

See `weights/README.md` for the expected file locations.

## Runtime engines

- [TensorRT FP16 engines](https://drive.google.com/file/d/1xsGHO8v09NgkF-WJUbTigseT6CgzYTzB/view?usp=sharing)

Install the downloader and fetch the required files:

```bash
python -m pip install -r requirements-download.txt
python scripts/download_assets.py --kind datasets
python scripts/download_assets.py --kind models
```

The downloader checks file sizes and extracts only the files listed in the
manifests.

The archive contains the AdaBins and YOLOv8s TensorRT FP16 engines used by the
webcam runtime.

## Experimental video

- [On-device robotic picking experiments](https://drive.google.com/file/d/174v3R8E6w8fyYJErNAI6nMhg6NivN0I6/view?usp=sharing)

The video shows the robot-control GUI, on-device Depth and Detection results,
successful picking, and representative retry cases. The footage is shown at
2x speed.

## Jetson environment

- NVIDIA Jetson Orin NX
- Ubuntu 22.04
- NVIDIA L4T 36.3.0
- Python 3.10.12
- CUDA 12.2
- TensorRT 8.6.2
- cuDNN 8.9.4
- PyTorch 2.3.0 Jetson CUDA build
- NumPy 1.23.5
- Ultralytics 8.3.181
- OpenCV 4.8.0 with GStreamer support
- GStreamer 1.20.3

PyTorch, OpenCV, CUDA, cuDNN, TensorRT, and GStreamer must match the Jetson
software stack. The generic `opencv-python` wheel is not suitable because the
webcam pipeline requires an OpenCV build with GStreamer enabled.

Install the direct Python dependencies with:

```bash
python -m pip install --no-deps -r requirements-runtime.txt
```

## Webcam and TensorRT inference

Required hardware and network settings:

- CUDA-capable GPU
- `/dev/video0`
- YUY2 webcam at 640x480 and 30 fps
- TCP server address `192.168.55.1:12345`

Create the runtime directories and start the program:

```bash
mkdir -p yolo_debug
mkdir -p tensorrt_deployment/artifacts/staging/ultralytics_config
python test_webcam_adabins_yolo_tensorRT_fp16.py --cam-idx 0
```

The client sends the ASCII string `True` to request one inference result. The
server responds with `[UTF-8 JSON + newline][Detection PNG][Depth PNG]`. The
JSON fields `YoloImageLength` and `DepthImageLength` give the exact byte counts
of the two images.

`--yolo-engine` can be used to select another Detection engine.

TensorRT engines are dependent on the Jetson model, GPU architecture, CUDA,
and TensorRT version. A different Jetson environment may require exporting
ONNX models and rebuilding the engines.

## GPIO and UART

Hardware settings:

- `/dev/ttyTHS1`, 115200 baud, 8N1, flow control off
- 3.3 V TTL UART with common ground
- BOARD 8: TX
- BOARD 10: RX
- User account in the `dialout` and `gpio` groups
- Jetson.GPIO udev rules and carrier-board pinmux configuration

Run the program only after checking the wiring:

```bash
python ard_test_GPIO.py
```

The program uses 11-byte UART request and response frames. It opens GPIO and
UART during startup and should not be imported as a hardware test.

`ard_test_GPIO.py` uses BOARD pin 32 for the Stopper. An alternate source
version and older wiring notes use BOARD pin 31. Confirm the actual wiring and
pinmux before operating the hardware.

## Windows GUI

`Robot_GUI/` targets .NET Framework 4.7.2 and uses Newtonsoft.Json 13.0.3. The
default connections are:

| Interface | Default | Configuration |
| --- | --- | --- |
| Delta robot | `COM3` | 115200 baud, 8N1 |
| Jetson UART bridge | `COM5` | 115200 baud, 8N1 |
| Jetson camera server | `192.168.55.1:12345` | TCP |

Build and protocol-test instructions are in `Robot_GUI/README.md`.

## License

See `LICENSE` and `THIRD_PARTY_NOTICES.md` for the current project and
third-party terms.
