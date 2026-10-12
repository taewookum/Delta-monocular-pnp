# Datasets

The dataset archives are hosted separately from the code repository.

| Dataset | Train | Validation | Test | Total | Archive size |
| --- | ---: | ---: | ---: | ---: | ---: |
| Depth | 30,641 pairs | 864 pairs | 1,097 pairs | 32,602 pairs | 22,089,590,042 bytes |
| Detection | 3,381 pairs | 199 pairs | 402 pairs | 3,982 pairs | 253,987,435 bytes |

- [Depth dataset](https://drive.google.com/file/d/1HvFzt6cVB4kj__SZr1kLksfSv2X6MWPh/view?usp=sharing)
- [Detection dataset](https://drive.google.com/file/d/1-wUX-Eqa-RUqGxmrVZ9NkU0q-ULWhb7A/view?usp=sharing)

The 1,097-pair Depth test set was captured with a separate sensor (D435i) and
randomly sampled; it is disjoint from the training and validation splits. The
Detection test split contains 402 images.

Download both archives with:

```bash
python -m pip install -r requirements-download.txt
python scripts/download_assets.py --kind datasets
```

The archives are saved under `downloads/data/`. If Google Drive reports a
download quota error, retry after the quota resets.
