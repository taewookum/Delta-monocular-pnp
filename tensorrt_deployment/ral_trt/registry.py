from __future__ import annotations

from dataclasses import dataclass, replace
from pathlib import Path
from typing import Literal

from .paths import (
    BUNDLE_ROOT,
    ENGINE_ROOT,
    ONNX_ROOT,
    THIRD_PARTY_ROOT,
    WORKSPACE_ROOT,
)


Family = Literal["depth", "detection"]


@dataclass(frozen=True)
class ModelSpec:
    id: str
    family: Family
    source_dir: str
    source_commit: str
    checkpoint_rel: str
    input_hw: tuple[int, int]
    mean: tuple[float, float, float]
    std: tuple[float, float, float]
    output_contract: str
    default_precision: Literal["fp16", "fp32"] = "fp16"
    checkpoint_base: Literal["bundle", "workspace"] = "bundle"
    auxiliary_sources: tuple[tuple[str, str], ...] = ()
    alias_of: str | None = None
    raw_rgb_input: bool = False

    @property
    def canonical_id(self) -> str:
        return self.alias_of or self.id

    @property
    def checkpoint(self) -> Path:
        root = BUNDLE_ROOT if self.checkpoint_base == "bundle" else WORKSPACE_ROOT
        return root / self.checkpoint_rel

    @property
    def source_root(self) -> Path:
        return THIRD_PARTY_ROOT / self.source_dir

    @property
    def onnx_path(self) -> Path:
        return ONNX_ROOT / f"{self.id}.onnx"

    @property
    def engine_path(self) -> Path:
        return ENGINE_ROOT / f"{self.id}_{self.default_precision}.engine"


IMAGENET_MEAN = (0.485, 0.456, 0.406)
IMAGENET_STD = (0.229, 0.224, 0.225)
HALF_MEAN = (0.5, 0.5, 0.5)
HALF_STD = (0.5, 0.5, 0.5)
IDENTITY_MEAN = (0.0, 0.0, 0.0)
IDENTITY_STD = (1.0, 1.0, 1.0)


DEPTH_SPECS: dict[str, ModelSpec] = {
    spec.id: spec
    for spec in (
        ModelSpec(
            "idisc_swinl",
            "depth",
            "idisc",
            "5d6cea2f0e5236701b2dfd6dac89cde13a328d33",
            "depth/idisc/checkpoint/idisc_swinl_ral_best_model_fp32.pt",
            (480, 640),
            IMAGENET_MEAN,
            IMAGENET_STD,
            "metric depth in metres, Bx1x480x640",
        ),
        ModelSpec(
            "newcrfs_large07",
            "depth",
            "newcrfs",
            "d327bf7ca8fb43959734bb02ddc7b56cf283c8d9",
            "depth/newcrfs/checkpoint/newcrfs_large07_ral_best_model_fp32.pt",
            (480, 640),
            IMAGENET_MEAN,
            IMAGENET_STD,
            "metric depth in metres, Bx1x480x640",
        ),
        ModelSpec(
            "pixelformer_large07",
            "depth",
            "pixelformer",
            "bb0ae9f3108666328c406c500497ddedc4479891",
            "depth/pixelformer/checkpoint/pixelformer_large07_ral_best_model_fp32.pt",
            (480, 640),
            IMAGENET_MEAN,
            IMAGENET_STD,
            "metric depth in metres, Bx1x480x640",
        ),
        ModelSpec(
            "iebins_large07",
            "depth",
            "iebins",
            "792bf1ad13326ff73e1df0723ba1d87d6dcbd218",
            "depth/iebins/checkpoint/iebins_large07_ral_best_model_fp32.pt",
            (480, 640),
            IMAGENET_MEAN,
            IMAGENET_STD,
            "last iterative metric-depth stage, Bx1x480x640",
        ),
        ModelSpec(
            "dpt_hybrid_metric",
            "depth",
            "dpt",
            "cd3fe90bb4c48577535cc4d51b602acca688a2ee",
            "depth/dpt_hybrid/checkpoint/dpt_hybrid_ral_metric_best_model_fp32.pt",
            (384, 512),
            HALF_MEAN,
            HALF_STD,
            "metric mapping then bicubic depth resize, Bx1x480x640",
            "fp32",
        ),
        ModelSpec(
            "midas_beit_base_384_metric",
            "depth",
            "midas",
            "1645b7e1675301fdfac03640738fe5a6531e17d6",
            "depth/midas_beit_base_384/checkpoint/midas_beit_base_384_ral_metric_best_fp32.pt",
            (384, 512),
            HALF_MEAN,
            HALF_STD,
            "bicubic relative resize then metric mapping, Bx1x480x640",
        ),
        ModelSpec(
            "fastdepth_unpruned_640x480",
            "depth",
            "fastdepth",
            "e68492011609c9bfb7de6d402da5d1d201d95bd9",
            "depth/fastdepth_640x480/checkpoint/fastdepth_unpruned_ral_640x480_best_fp32.pt",
            (480, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "metric depth in metres, Bx1x480x640",
        ),
        ModelSpec(
            "guidedepth_s",
            "depth",
            "guideddecoding",
            "b1db5156e2aa07544dfeef0625bf73209ee50c4c",
            "depth/guidedepth_s/checkpoint/guidedepth_s_ral_best_state_dict_fp32.pth",
            (480, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "depth_m=clamp(10/prediction_inverse, 0.1, 10), Bx1x480x640",
        ),
    )
}


# AdaBins was intentionally excluded from the August 2026 model bundle. Keep
# both the deployed paper-era checkpoint and the later e20 fine-tuned
# checkpoint separate from the bundle registry so bundle integrity still means
# exactly 8 depth + 5 YOLO.
LEGACY_DEPTH_SPECS: dict[str, ModelSpec] = {
    "adabins_ft": ModelSpec(
        "adabins_ft",
        "depth",
        "adabins",
        "0952d91e9e762be310bb4cd055cbfe2448c0ce20",
        "AdaBins_0918_0911_0901_merge_0723_val_0901_val_100_epoch24_best.pt",
        (480, 640),
        IMAGENET_MEAN,
        IMAGENET_STD,
        "deployed AdaBins metric depth resized bilinearly to Bx1x480x640",
        checkpoint_base="workspace",
        auxiliary_sources=(
            (
                "gen-efficientnet-pytorch",
                "771ce082b2ce6d033f55b3d47c1f77389ad3c180",
            ),
        ),
    ),
    "adabins_ft_e20": ModelSpec(
        "adabins_ft_e20",
        "depth",
        "adabins",
        "0952d91e9e762be310bb4cd055cbfe2448c0ce20",
        "AdaBins_best_abs_rel_e20_seed42.pt",
        (480, 640),
        IMAGENET_MEAN,
        IMAGENET_STD,
        "fine-tuned e20 AdaBins metric depth resized bilinearly to Bx1x480x640",
        checkpoint_base="workspace",
        auxiliary_sources=(
            (
                "gen-efficientnet-pytorch",
                "771ce082b2ce6d033f55b3d47c1f77389ad3c180",
            ),
        ),
    ),
}


BASE_DEPTH_SPECS = DEPTH_SPECS | LEGACY_DEPTH_SPECS


# Paper-comparison protocol: every engine receives the exact same external
# RGB float32 [0, 1] tensor with NCHW shape 1x3x480x640. Model-specific
# normalization is part of the exported graph for these aliases. Separate IDs
# preserve the native-resolution deployment artifacts.
EVAL_DEPTH_SPECS: dict[str, ModelSpec] = {}
for _base_id, _base_spec in BASE_DEPTH_SPECS.items():
    _eval_id = f"{_base_id}_eval_rgb480x640"
    EVAL_DEPTH_SPECS[_eval_id] = replace(
        _base_spec,
        id=_eval_id,
        input_hw=(480, 640),
        output_contract=(
            "external raw RGB float32 [0,1], Bx3x480x640; "
            f"internal normalization; {_base_spec.output_contract}"
        ),
        alias_of=_base_id,
        raw_rgb_input=True,
    )

# The native DPT artifact remains FP32.  For the fixed RGB480x640 evaluation
# contract, mixed FP16 was separately probed on the real evaluation tensor and
# retained finite/positive output with full parity at the registered tolerance.
_dpt_eval_id = "dpt_hybrid_metric_eval_rgb480x640"
EVAL_DEPTH_SPECS[_dpt_eval_id] = replace(
    EVAL_DEPTH_SPECS[_dpt_eval_id], default_precision="fp16"
)


DEPLOY_DEPTH_SPECS = BASE_DEPTH_SPECS | EVAL_DEPTH_SPECS


YOLO_SPECS: dict[str, ModelSpec] = {
    spec.id: spec
    for spec in (
        ModelSpec(
            "yolov5u_s",
            "detection",
            "ultralytics",
            "8eac23498e68dfc5bca24231a471421dcff21d18",
            "yolo/yolov5u_s/checkpoint/yolov5u_s_ral_apple_best_fp32.pt",
            (640, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "raw Ultralytics output; host class-aware NMS",
        ),
        ModelSpec(
            "yolov8s",
            "detection",
            "ultralytics",
            "8eac23498e68dfc5bca24231a471421dcff21d18",
            "yolo/yolov8s/checkpoint/yolov8s_ral_apple_best_fp32.pt",
            (640, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "raw Ultralytics output; host class-aware NMS",
        ),
        ModelSpec(
            "yolov9s",
            "detection",
            "ultralytics",
            "8eac23498e68dfc5bca24231a471421dcff21d18",
            "yolo/yolov9s/checkpoint/yolov9s_ral_apple_best_fp32.pt",
            (640, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "raw Ultralytics output; host class-aware NMS",
        ),
        ModelSpec(
            "yolov10s",
            "detection",
            "ultralytics",
            "8eac23498e68dfc5bca24231a471421dcff21d18",
            "yolo/yolov10s/checkpoint/yolov10s_ral_apple_best_fp32.pt",
            (640, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "native end-to-end NMS-free top-k output; do not add legacy NMS",
        ),
        ModelSpec(
            "yolov11s",
            "detection",
            "ultralytics",
            "8eac23498e68dfc5bca24231a471421dcff21d18",
            "yolo/yolov11s/checkpoint/yolov11s_ral_apple_best_fp32.pt",
            (640, 640),
            IDENTITY_MEAN,
            IDENTITY_STD,
            "raw Ultralytics output; host class-aware NMS",
        ),
    )
}


EVAL_YOLO_SPECS: dict[str, ModelSpec] = {}
for _base_id, _base_spec in YOLO_SPECS.items():
    _eval_id = f"{_base_id}_eval_rgb480x640"
    EVAL_YOLO_SPECS[_eval_id] = replace(
        _base_spec,
        id=_eval_id,
        input_hw=(480, 640),
        output_contract=(
            "external raw RGB float32 [0,1], Bx3x480x640; "
            f"{_base_spec.output_contract}"
        ),
        alias_of=_base_id,
        raw_rgb_input=True,
    )


DEPLOY_YOLO_SPECS = YOLO_SPECS | EVAL_YOLO_SPECS


ALL_SPECS = DEPTH_SPECS | YOLO_SPECS
DEPLOY_ALL_SPECS = DEPLOY_DEPTH_SPECS | DEPLOY_YOLO_SPECS


def get_spec(model_id: str) -> ModelSpec:
    try:
        return DEPLOY_ALL_SPECS[model_id]
    except KeyError as error:
        choices = ", ".join(sorted(DEPLOY_ALL_SPECS))
        raise KeyError(f"unknown model {model_id!r}; choose one of: {choices}") from error
