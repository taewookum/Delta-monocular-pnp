#!/usr/bin/env python
import sys, os, argparse, time
from pathlib import Path
from typing import Any, Dict, List, Tuple

SCRIPT_DIR = Path(__file__).resolve().parent
os.environ.setdefault(
    "YOLO_CONFIG_DIR",
    str(SCRIPT_DIR / "tensorrt_deployment/artifacts/staging/ultralytics_config"),
)

sys.path.insert(0, "/usr/lib/python3.10/dist-packages")
import cv2
import numpy as np

import torch
import torch.cuda as tcuda

from ultralytics import YOLO
from tensorrt_deployment.ral_trt.runtime import TensorRTEngine

# TCP
import socket, select, json

# ──────────────────────────────────────
SNAP_INTERVAL = 1.0

ADABINS_TRT_ENGINE = (
    SCRIPT_DIR
    / "tensorrt_deployment/artifacts/engines/"
      "adabins_ft_eval_rgb480x640_fp16.engine"
)
YOLO_TRT_ENGINE = (
    SCRIPT_DIR
    / "tensorrt_deployment/artifacts/engines/yolov8s_fp16.engine"
)
YOLO_DEBUG_IMAGE = SCRIPT_DIR / "yolo_debug/yolo_boxes_latest.png"
DEPTH_DEBUG_IMAGE = SCRIPT_DIR / "yolo_debug/depth_magma_latest.png"
DEBUG_CROP_X1, DEBUG_CROP_Y1 = 209, 137
DEBUG_CROP_X2, DEBUG_CROP_Y2 = 508, 340
YOLO_APPLE_CLASS_ID = 0
YOLO_CONFIDENCE_THRESHOLD = 0.522
ADABINS_WARMUP_ITERATIONS = 3
YOLO_WARMUP_ITERATIONS = 1
WORK_AREA_X1, WORK_AREA_Y1 = DEBUG_CROP_X1, DEBUG_CROP_Y1
WORK_AREA_X2, WORK_AREA_Y2 = DEBUG_CROP_X2, DEBUG_CROP_Y2

torch.backends.cudnn.benchmark = True
torch.backends.cuda.matmul.allow_tf32 = True
torch.backends.cudnn.allow_tf32 = True

# ──────────────────────────────────────
def Adabins_transform(image_rgb: np.ndarray) -> torch.Tensor:
    """Convert an RGB image to a TensorRT input tensor."""
    if image_rgb.dtype != np.float32:
        image_rgb = image_rgb.astype(np.float32) / 255.0
    return torch.from_numpy(
        np.ascontiguousarray(image_rgb.transpose(2, 0, 1))
    ).float()

def ROI_coord(box_int: torch.Tensor) -> List[Tuple[int, int, int, int]]:
    """Return center ROIs for boxes inside the configured work area."""
    rois: List[Tuple[int, int, int, int]] = []
    for x1, y1, x2, y2 in box_int.tolist():
        cx, cy = (x1 + x2) // 2, (y1 + y2) // 2
        dw, dh = int((x2 - x1) * 0.25), int((y2 - y1) * 0.25)
        rx1, ry1 = cx - dw, cy - dh
        rx2, ry2 = cx + dw, cy + dh
        if (
            rx1 >= WORK_AREA_X1
            and ry1 >= WORK_AREA_Y1
            and rx2 <= WORK_AREA_X2
            and ry2 <= WORK_AREA_Y2
        ):
            rois.append((rx1, ry1, rx2, ry2))
    return rois

def compute_roi_depth_means(depth: torch.Tensor, rois) -> List[float]:
    depth_np = depth.squeeze().detach().cpu().numpy()
    h, w = depth_np.shape
    means = []
    for x1, y1, x2, y2 in rois:
        x1, x2 = max(0, x1), min(w, x2)
        y1, y2 = max(0, y1), min(h, y2)
        roi = depth_np[y1:y2, x1:x2]
        valid = roi[np.isfinite(roi) & (roi > 0)]
        means.append(float(np.mean(valid)) if valid.size else float('inf'))
    return means

def final_coordinate(rois, means):
    """Return up to two nearest ROI centers and their mean depths."""
    if not rois or not means or len(rois) != len(means):
        return None
    if len(means) == 1:
        x1, y1, x2, y2 = rois[0]
        return (x1 + x2) // 2, (y1 + y2) // 2, means[0]
    sorted_idx = np.argsort(means)
    idx1, idx2 = sorted_idx[:2]
    results = []
    for idx in (idx1, idx2):
        x1, y1, x2, y2 = rois[idx]
        results.append(((x1 + x2) // 2, (y1 + y2) // 2, means[idx]))
    return results

def filter_boxes_by_center(boxes_int: torch.Tensor) -> torch.Tensor:
    cx = (boxes_int[:, 0] + boxes_int[:, 2]) // 2
    cy = (boxes_int[:, 1] + boxes_int[:, 3]) // 2
    mask = (
        (cx >= WORK_AREA_X1)
        & (cx <= WORK_AREA_X2)
        & (cy >= WORK_AREA_Y1)
        & (cy <= WORK_AREA_Y2)
    )
    return boxes_int[mask]

def crop_debug_image(image: np.ndarray) -> np.ndarray:
    """Crop the configured work area from a processed frame."""
    height, width = image.shape[:2]
    if not (
        0 <= DEBUG_CROP_X1 < DEBUG_CROP_X2 <= width
        and 0 <= DEBUG_CROP_Y1 < DEBUG_CROP_Y2 <= height
    ):
        raise ValueError(
            "debug crop is outside image bounds: "
            f"crop=({DEBUG_CROP_X1},{DEBUG_CROP_Y1})-"
            f"({DEBUG_CROP_X2},{DEBUG_CROP_Y2}), image={width}x{height}"
        )
    return image[
        DEBUG_CROP_Y1:DEBUG_CROP_Y2,
        DEBUG_CROP_X1:DEBUG_CROP_X2,
    ].copy()

def save_yolo_debug_image(
    frame_bgr: np.ndarray,
    all_boxes_int: torch.Tensor,
    targets: List[Tuple[int, int, float]],
) -> bytes:
    """Save detection boxes, target centers, and primary depth."""
    if all_boxes_int.numel() == 0 or not targets:
        return b""

    try:
        YOLO_DEBUG_IMAGE.parent.mkdir(parents=True, exist_ok=True)
        debug_frame = frame_bgr.copy()
        for x1, y1, x2, y2 in all_boxes_int.tolist():
            cv2.rectangle(
                debug_frame,
                (x1, y1),
                (x2, y2),
                (0, 255, 0),
                2,
                lineType=cv2.LINE_AA,
            )

        for target_index, (cx, cy, depth) in enumerate(targets, start=1):
            center_color = (255, 0, 0) if target_index == 1 else (255, 255, 0)
            cv2.circle(
                debug_frame,
                (cx, cy),
                4,
                center_color,
                -1,
                lineType=cv2.LINE_AA,
            )

            if target_index != 1:
                continue

            label = f"{depth:.3f} m"
            font = cv2.FONT_HERSHEY_SIMPLEX
            font_scale = 0.6
            thickness = 2
            (text_width, text_height), _ = cv2.getTextSize(
                label, font, font_scale, thickness
            )
            text_x = max(
                DEBUG_CROP_X1 + 2,
                min(cx + 8, DEBUG_CROP_X2 - text_width - 2),
            )
            text_y = min(
                max(DEBUG_CROP_Y1 + text_height + 2, cy - 8),
                DEBUG_CROP_Y2 - 2,
            )
            cv2.putText(
                debug_frame,
                label,
                (text_x, text_y),
                font,
                font_scale,
                (255, 0, 0),
                thickness,
                lineType=cv2.LINE_AA,
            )

        cropped_debug_frame = crop_debug_image(debug_frame)
        if cv2.imwrite(str(YOLO_DEBUG_IMAGE), cropped_debug_frame):
            print(f"[YOLO] target image saved: {YOLO_DEBUG_IMAGE}")
            return YOLO_DEBUG_IMAGE.read_bytes()
        else:
            print(f"[YOLO] target image save failed: {YOLO_DEBUG_IMAGE}")
    except Exception as error:
        print(f"[YOLO] target image save error: {error}")
    return b""

def depth_to_magma_r(depth_np: np.ndarray) -> np.ndarray:
    """Convert metric depth to a frame-normalized Magma BGR image."""
    valid_mask = np.isfinite(depth_np) & (depth_np > 0)
    depth_u8 = np.zeros(depth_np.shape, dtype=np.uint8)
    valid_depth = depth_np[valid_mask]

    if valid_depth.size:
        depth_min = float(valid_depth.min())
        depth_max = float(valid_depth.max())
        if depth_max > depth_min:
            normalized = (depth_np[valid_mask] - depth_min) / (
                depth_max - depth_min
            )
            depth_u8[valid_mask] = np.uint8(
                np.clip((1.0 - normalized) * 255.0, 0.0, 255.0)
            )

    depth_magma_r = cv2.applyColorMap(depth_u8, cv2.COLORMAP_MAGMA)
    depth_magma_r[~valid_mask] = 0
    return depth_magma_r

def save_depth_debug_image(depth_magma: np.ndarray) -> bytes:
    """Save the Magma depth image for the latest target frame."""
    try:
        DEPTH_DEBUG_IMAGE.parent.mkdir(parents=True, exist_ok=True)
        cropped_depth_magma = crop_debug_image(depth_magma)
        if cv2.imwrite(str(DEPTH_DEBUG_IMAGE), cropped_depth_magma):
            print(f"[Depth] magma_r image saved: {DEPTH_DEBUG_IMAGE}")
            return DEPTH_DEBUG_IMAGE.read_bytes()
        else:
            print(f"[Depth] magma_r image save failed: {DEPTH_DEBUG_IMAGE}")
    except Exception as error:
        print(f"[Depth] magma_r image save error: {error}")
    return b""

def send_grip_response(
    client_socket,
    payload: Dict[str, Any],
    yolo_png: bytes = b"",
    depth_png: bytes = b"",
) -> None:
    """Send a JSON header followed by two length-delimited PNG images."""
    response_header = dict(payload)
    response_header.update(
        {
            "ImageProtocol": "json-line+png-v1",
            "ImageEncoding": "png",
            "YoloImageLength": len(yolo_png),
            "DepthImageLength": len(depth_png),
            "ImageWidth": DEBUG_CROP_X2 - DEBUG_CROP_X1,
            "ImageHeight": DEBUG_CROP_Y2 - DEBUG_CROP_Y1,
            "ImageCropX1": DEBUG_CROP_X1,
            "ImageCropY1": DEBUG_CROP_Y1,
            "ImageCropX2": DEBUG_CROP_X2,
            "ImageCropY2": DEBUG_CROP_Y2,
        }
    )
    header_bytes = (
        json.dumps(response_header, separators=(",", ":")) + "\n"
    ).encode("utf-8")
    client_socket.sendall(header_bytes)
    if yolo_png:
        client_socket.sendall(yolo_png)
    if depth_png:
        client_socket.sendall(depth_png)
    print(
        "[TCP] grip response sent: "
        f"header={len(header_bytes)} bytes, "
        f"YOLO={len(yolo_png)} bytes, Depth={len(depth_png)} bytes"
    )

def open_camera_gst(width=640, height=480, fps=30, device="/dev/video0", format="YUY2"):
    if format == "YUY2":
        pipe = (
            f"v4l2src device={device} io-mode=2 do-timestamp=true ! "
            f"video/x-raw,format=YUY2,width={width},height={height},framerate={fps}/1 ! "
            f"videoconvert ! video/x-raw,format=BGR ! "
            f"appsink drop=true sync=false max-buffers=1"
        )
    elif format == "MJPEG":
        pipe = (
            f"v4l2src device={device} io-mode=2 do-timestamp=true ! "
            f"image/jpeg,width={width},height={height},framerate={fps}/1 ! "
            f"nvjpegdec ! videoconvert ! video/x-raw,format=BGR ! "
            f"appsink drop=true sync=false max-buffers=1"
        )
    else:
        raise ValueError("Unsupported camera format.")
    cap = cv2.VideoCapture(pipe, cv2.CAP_GSTREAMER)
    return cap

# ──────────────────────────────────────
def build_models(device, yolo_warmup_frame=None,
                 yolo_engine: Path = YOLO_TRT_ENGINE):
    if device.type != "cuda":
        raise RuntimeError("CUDA is required to run the TensorRT models.")
    if not ADABINS_TRT_ENGINE.is_file():
        raise FileNotFoundError(ADABINS_TRT_ENGINE)
    yolo_engine = Path(yolo_engine).expanduser().resolve()
    if not yolo_engine.is_file():
        raise FileNotFoundError(yolo_engine)

    adabins = TensorRTEngine(ADABINS_TRT_ENGINE, device="cuda:0")
    warmup_input = torch.zeros(
        (1, 3, 480, 640), dtype=torch.float32, device="cuda:0"
    )
    for _ in range(ADABINS_WARMUP_ITERATIONS):
        warmup_outputs, _ = adabins.infer_timed(warmup_input)
        if not torch.isfinite(warmup_outputs["depth"]).all():
            raise RuntimeError("AdaBins TensorRT FP16 warm-up produced NaN/Inf")
    del warmup_outputs, warmup_input
    yolo = YOLO(str(yolo_engine), task="detect")
    if yolo_warmup_frame is None:
        yolo_warmup_frame = np.zeros((480, 640, 3), dtype=np.uint8)
    for _ in range(YOLO_WARMUP_ITERATIONS):
        yolo.predict(
            source=yolo_warmup_frame, imgsz=640,
            classes=[YOLO_APPLE_CLASS_ID], conf=YOLO_CONFIDENCE_THRESHOLD,
            verbose=False, device=0
        )
    del yolo_warmup_frame
    print(f"[TensorRT] AdaBins: {ADABINS_TRT_ENGINE}")
    print(f"[TensorRT] AdaBins warm-up: {ADABINS_WARMUP_ITERATIONS} iterations")
    print(f"[TensorRT] YOLO: {yolo_engine}")
    print(f"[TensorRT] YOLO warm-up: {YOLO_WARMUP_ITERATIONS} iteration")
    return adabins, yolo

# ──────────────────────────────────────
@torch.inference_mode()
def one_snapshot_process(frame_bgr, device, idisc, yolo):
    """Return the visualization, targets, detection PNG, and depth PNG."""
    if frame_bgr is None:
        print("Frame capture failed. Check the camera and GStreamer pipeline.")
        return frame_bgr, [], b"", b""

    frame_rgb = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB)
    depth_in = (
        Adabins_transform(frame_rgb)
        .unsqueeze(0)
        .to(device=device, dtype=torch.float32, non_blocking=True)
        .contiguous()
    )  # [1,3,480,640], raw RGB [0,1]

    pipeline_started = time.perf_counter()
    depth_outputs, depth_inference_ms = idisc.infer_timed(depth_in)
    depth_pred = depth_outputs["depth"]  # [1,1,480,640], metric depth (m)
    depth_is_valid = bool(torch.isfinite(depth_pred).all().item())
    results = yolo.predict(
        source=frame_bgr, imgsz=640,
        classes=[YOLO_APPLE_CLASS_ID], conf=YOLO_CONFIDENCE_THRESHOLD,
        verbose=False,
        device=0
    )
    tcuda.synchronize(device)
    pipeline_ms = (time.perf_counter() - pipeline_started) * 1000.0
    yolo_inference_ms = None
    if results:
        yolo_inference_ms = results[0].speed.get("inference")
    yolo_text = (
        f"{float(yolo_inference_ms):.3f} ms"
        if yolo_inference_ms is not None
        else "N/A"
    )
    print(
        f"[Timing] AdaBins TRT={depth_inference_ms:.3f} ms, "
        f"YOLO TRT={yolo_text}, model pipeline={pipeline_ms:.3f} ms"
    )

    if not results:
        return frame_bgr, [], b"", b""
    r0 = results[0]

    if not depth_is_valid:
        print("[TensorRT] AdaBins depth contains NaN/Inf; target output suppressed")
        return frame_bgr, [], b"", b""

    if (getattr(r0, 'boxes', None) is None) or (r0.boxes is None) or (r0.boxes.xyxy is None) or (r0.boxes.xyxy.shape[0] == 0):
        return frame_bgr, [], b"", b""

    all_boxes_int = torch.round(r0.boxes.xyxy).int().cpu()
    boxes_int = filter_boxes_by_center(all_boxes_int)

    depth_map = depth_pred.float()

    rois = ROI_coord(boxes_int)
    means = compute_roi_depth_means(depth_map, rois)
    res = final_coordinate(rois, means)
    if res is None:
        return frame_bgr, [], b"", b""

    targets = res if isinstance(res, list) else [res]
    targets = [(int(cx), int(cy), float(d)) for (cx, cy, d) in targets]
    yolo_png = save_yolo_debug_image(frame_bgr, all_boxes_int, targets)

    draw = frame_bgr.copy()
    for (x1, y1, x2, y2) in boxes_int.tolist():
        cv2.rectangle(draw, (x1,y1), (x2,y2), (0,255,0), 2)

    if len(targets) >= 1:
        cx1, cy1, d1 = targets[0]
        cv2.circle(draw, (cx1, cy1), 6, (255,0,0), -1)
        cv2.putText(draw, f"{d1:.3f} m", (cx1+8, cy1-8), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255,0,0), 2)
    if len(targets) == 2:
        cx2, cy2, d2 = targets[1]
        cv2.circle(draw, (cx2, cy2), 5, (255,255,0), -1)

    depth_np = depth_map.squeeze().cpu().numpy()

    depth_color = depth_to_magma_r(depth_np)  # (480,640,3), BGR
    depth_png = save_depth_debug_image(depth_color)

    vis_concat = cv2.hconcat([draw, depth_color])

    return vis_concat, targets, yolo_png, depth_png
    # depth_np = np.nan_to_num(depth_np, nan=0.0, posinf=0.0, neginf=0.0)
    # dn = (depth_np - depth_np.min()) / (depth_np.max() - depth_np.min() + 1e-8)
    # depth8 = np.uint8(np.clip(dn*255, 0, 255))
    # depth_color = cv2.applyColorMap(depth8, cv2.COLORMAP_INFERNO)
    # depth_color = cv2.resize(depth_color, (draw.shape[1]//2, draw.shape[0]//2))
    # h, w, _ = depth_color.shape
    # # draw[0:h, 0:w] = depth_color

    # return draw, targets

# ──────────────────────────────────────
def main(cam_idx: int = 0, yolo_engine: Path = YOLO_TRT_ENGINE):
    device = torch.device("cuda" if tcuda.is_available() else "cpu")

    cap = open_camera_gst(width=640, height=480, fps=30,
                          device=f"/dev/video{cam_idx}", format="YUY2")
    if not cap.isOpened():
        print("Unable to open the camera."); return
    try:
        for _ in range(4):
            cap.grab()
        warmup_ok, yolo_warmup_frame = cap.read()
        if not warmup_ok or yolo_warmup_frame is None:
            raise RuntimeError("YOLO TensorRT warm-up camera frame capture failed")
        idisc, yolo = build_models(device, yolo_warmup_frame, yolo_engine)
    except Exception:
        cap.release()
        raise

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind(("192.168.55.1", 12345))
    server_socket.listen(1)
    print("[TCP] listening on ...")
    client_socket, client_address = server_socket.accept()
    print(f"[TCP] connection from {client_address}")
    print("Waiting for the GUI trigger. Send 'True' to process one frame; press Q to exit.")

    try:
        with torch.inference_mode():
            while True:
                if cv2.waitKey(1) & 0xFF == ord('q'):
                    break

                try:
                    data = client_socket.recv(1024)
                except ConnectionResetError:
                    print("[TCP] client reset connection; waiting for reconnect")
                    client_socket.close()
                    client_socket, client_address = server_socket.accept()
                    print(f"[TCP] reconnected from {client_address}")
                    continue
                if not data:
                    print("[TCP] client disconnected")
                    client_socket.close()
                    client_socket, client_address = server_socket.accept()
                    print(f"[TCP] reconnected from {client_address}")
                    continue

                msg = data.decode(errors="ignore").strip()
                if msg != "True":
                    continue

                start = time.perf_counter()
                for _ in range(4):
                    cap.grab()
                ret, frame_bgr = cap.read()
                if not ret:
                    elapsed = time.perf_counter() - start
                    payload = {"CamX_1": 0, "CamY_1": 0, "CamDepth_1": 0.0,
                               "CamX_2": 0, "CamY_2": 0, "CamDepth_2": 0.0,
                               "JetsonTime": float(elapsed)}
                    send_grip_response(client_socket, payload)
                    continue

                # T = time.time()
                vis, targets, yolo_png, depth_png = one_snapshot_process(
                    frame_bgr, device, idisc, yolo
                )
                # TT = time.time()
                elapsed = time.perf_counter() - start

                if len(targets) == 0:
                    payload = {"CamX_1": 0, "CamY_1": 0, "CamDepth_1": 0.0,
                               "CamX_2": 0, "CamY_2": 0, "CamDepth_2": 0.0,
                               "JetsonTime": float(elapsed)}
                elif len(targets) == 1:
                    cx1, cy1, d1 = targets[0]
                    payload = {"CamX_1": cx1, "CamY_1": cy1, "CamDepth_1": d1,
                               "CamX_2": cx1, "CamY_2": cy1, "CamDepth_2": d1,
                               "JetsonTime": float(elapsed)}
                else:  # len == 2
                    (cx1, cy1, d1), (cx2, cy2, d2) = targets[:2]
                    payload = {"CamX_1": cx1, "CamY_1": cy1, "CamDepth_1": d1,
                               "CamX_2": cx2, "CamY_2": cy2, "CamDepth_2": d2,
                               "JetsonTime": float(elapsed)}

                send_grip_response(client_socket, payload, yolo_png, depth_png)

                # if vis is not None:
                #     cv2.imshow("Snapshot TCP", vis)

    finally:
        try: client_socket.close()
        except: pass
        try: server_socket.close()
        except: pass
        cap.release()
        cv2.destroyAllWindows()
        print("Snapshot TCP server stopped.")

# ──────────────────────────────────────
if __name__ == "__main__":
    parser = argparse.ArgumentParser("IDisc+YOLO snapshot TCP server",
                                     formatter_class=argparse.ArgumentDefaultsHelpFormatter)
    parser.add_argument("--cam-idx", type=int, default=0, help="Webcam index")
    parser.add_argument(
        "--yolo-engine", type=Path, default=YOLO_TRT_ENGINE,
        help="Path to the fine-tuned YOLOv8s TensorRT FP16 engine",
    )
    args = parser.parse_args()
    try:
        main(args.cam_idx, args.yolo_engine)
    except KeyboardInterrupt:
        print("\n[server] interrupted")
