import os
from pathlib import Path
from urllib.parse import quote_plus

import pandas as pd
from dotenv import load_dotenv
from sqlalchemy import create_engine


load_dotenv()


def create_sqlserver_engine():
    server = os.getenv("SQL_SERVER")
    database = os.getenv("SQL_DATABASE")
    driver = os.getenv("SQL_DRIVER", "ODBC Driver 17 for SQL Server")
    trusted_connection = os.getenv("SQL_TRUSTED_CONNECTION", "yes").lower()

    if trusted_connection == "yes":
        connection_string = (
            f"DRIVER={{{driver}}};"
            f"SERVER={server};"
            f"DATABASE={database};"
            f"Trusted_Connection=yes;"
            f"TrustServerCertificate=yes;"
        )
    else:
        username = os.getenv("SQL_USERNAME")
        password = os.getenv("SQL_PASSWORD")

        connection_string = (
            f"DRIVER={{{driver}}};"
            f"SERVER={server};"
            f"DATABASE={database};"
            f"UID={username};"
            f"PWD={password};"
            f"TrustServerCertificate=yes;"
        )

    encoded_connection = quote_plus(connection_string)
    return create_engine(f"mssql+pyodbc:///?odbc_connect={encoded_connection}")


def export_view(engine, view_name, output_name):
    output_dir = Path("data")
    output_dir.mkdir(exist_ok=True)

    print(f"Dang doc du lieu tu view: {view_name}")

    query = f"SELECT * FROM {view_name}"
    df = pd.read_sql(query, engine)

    print(f"So dong: {len(df)}")
    print(f"So cot: {len(df.columns)}")
    print("Danh sach cot:")
    print(list(df.columns))

    csv_path = output_dir / f"{output_name}.csv"
    parquet_path = output_dir / f"{output_name}.parquet"

    df.to_csv(csv_path, index=False, encoding="utf-8-sig")
    df.to_parquet(parquet_path, index=False)

    print(f"Da xuat CSV: {csv_path}")
    print(f"Da xuat Parquet: {parquet_path}")
    print("-" * 60)


def main():
    engine = create_sqlserver_engine()

    datasets = {
        "v_Dataset_DuBaoTreHan": "dataset_du_bao_tre_han",
        "v_Dataset_DeXuatGiaoViec": "dataset_de_xuat_giao_viec",
        "v_Dataset_PhatHienDiemNghen": "dataset_phat_hien_diem_nghen",
    }

    for view_name, output_name in datasets.items():
        export_view(engine, view_name, output_name)

    print("Hoan thanh ETL.")


if __name__ == "__main__":
    main()