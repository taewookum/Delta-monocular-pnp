from __future__ import annotations

from pathlib import Path


DEPLOY_ROOT = Path(__file__).resolve().parents[1]
WORKSPACE_ROOT = DEPLOY_ROOT.parent
BUNDLE_ROOT = (
    WORKSPACE_ROOT
    / "RAL_Jetson_Model_Bundle_20260804_v1"
    / "RAL_Jetson_Model_Bundle_20260804_v1"
)
MANIFEST_PATH = BUNDLE_ROOT / "MANIFEST.json"
THIRD_PARTY_ROOT = DEPLOY_ROOT / "third_party"
ARTIFACT_ROOT = DEPLOY_ROOT / "artifacts"
ONNX_ROOT = ARTIFACT_ROOT / "onnx"
ENGINE_ROOT = ARTIFACT_ROOT / "engines"
REPORT_ROOT = ARTIFACT_ROOT / "reports"
STAGING_ROOT = ARTIFACT_ROOT / "staging"
TRTEXEC = Path("/usr/src/tensorrt/bin/trtexec")


def ensure_artifact_directories() -> None:
    for path in (ONNX_ROOT, ENGINE_ROOT, REPORT_ROOT, STAGING_ROOT):
        path.mkdir(parents=True, exist_ok=True)
