"""
Compatibility shim: export DB loader functions expected as
`src.preprocessing.db_connector` by other modules.

This file forwards calls to `generate_synthetic_data.py` which contains
the actual connection and query helpers.
"""
from .generate_synthetic_data import (
    get_connection_string,
    get_connection,
    query_to_dataframe,
    load_dataset_du_bao_tre_han,
    load_dataset_de_xuat_giao_viec,
    load_dataset_phat_hien_diem_nghen,
    load_phu_thuoc_cong_viec,
    load_nguoi_dung,
)

__all__ = [
    "get_connection_string",
    "get_connection",
    "query_to_dataframe",
    "load_dataset_du_bao_tre_han",
    "load_dataset_de_xuat_giao_viec",
    "load_dataset_phat_hien_diem_nghen",
    "load_phu_thuoc_cong_viec",
    "load_nguoi_dung",
]
