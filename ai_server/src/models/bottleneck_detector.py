"""
Phân tích đồ thị (graph) phụ thuộc công việc để phát hiện điểm nghẽn (bottleneck).
Dùng dữ liệu thật từ bảng PhuThuocCongViec trong SQL Server.

Ghi chú: Bản này dùng NetworkX + graph centrality metrics (out-degree, betweenness,
PageRank) làm baseline. Khi có đủ dữ liệu lịch sử (nhãn "đã từng là bottleneck"
qua nhiều dự án), có thể nâng cấp lên GNN thực sự (PyTorch Geometric / DGL).

Chạy: python -m src.models.bottleneck_detector
"""
import pandas as pd
import networkx as nx
from pathlib import Path

PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"


def build_graph() -> nx.DiGraph:
    """Xây đồ thị có hướng: MaCongViecTruoc -> MaCongViecSau."""
    phu_thuoc_df = pd.read_csv(PROCESSED_DIR / "phu_thuoc_clean.csv")
    bottleneck_df = pd.read_csv(PROCESSED_DIR / "bottleneck_clean.csv")

    G = nx.DiGraph()
    for _, row in bottleneck_df.iterrows():
        G.add_node(
            row["MaCongViec"],
            trang_thai=row.get("TrangThaiHienTai"),
            so_gio_uoc_tinh=row.get("SoGioUocTinh"),
            nhan_diem_nghen=row.get("Nhan_GhiNhanDiemNghen"),
        )

    # for _, row in phu_thuoc_df.iterrows():
    #     truoc, sau = row["MaCongViecTruoc"], row["MaCongViecSau"]
    #     if truoc in G.nodes and sau in G.nodes:
    #         G.add_edge(truoc, sau, loai_phu_thuoc=row.get("LoaiPhuThuoc"))
    for _, row in phu_thuoc_df.iterrows():
        truoc, sau = row["MaCongViecTruoc"], row["MaCongViecSau"]
        for node in (truoc, sau):
            if node not in G.nodes:
                G.add_node(
                    node,
                    trang_thai=None,
                    so_gio_uoc_tinh=None,
                    nhan_diem_nghen=None,
                )
        G.add_edge(truoc, sau, loai_phu_thuoc=row.get("LoaiPhuThuoc"))


    return G


def detect_bottlenecks(G: nx.DiGraph, top_n: int = 10) -> list:
    """
    Phát hiện điểm nghẽn dựa trên 3 chỉ số:
    - out_degree cao: task này chặn nhiều task khác
    - betweenness centrality cao: task nằm trên nhiều đường đi quan trọng
    - PageRank cao: task có ảnh hưởng lan tỏa lớn trong đồ thị
    """
    if G.number_of_nodes() == 0:
        return []

    out_degree = dict(G.out_degree())
    betweenness = nx.betweenness_centrality(G)
    pagerank = nx.pagerank(G) if G.number_of_edges() > 0 else {n: 0 for n in G.nodes}
    max_out_degree = max(out_degree.values(), default=1) or 1

    results = []
    for node in G.nodes:
        bottleneck_score = (
            0.4 * (out_degree.get(node, 0) / max_out_degree)
            + 0.35 * betweenness.get(node, 0)
            + 0.25 * pagerank.get(node, 0)
        )
        results.append({
            "MaCongViec": int(node),
            "trang_thai": G.nodes[node].get("trang_thai"),
            "so_task_phu_thuoc_vao_no": out_degree.get(node, 0),
            "diem_trung_tam": round(betweenness.get(node, 0), 4),
            "diem_bottleneck": round(bottleneck_score * 100, 2),
        })

    results_sorted = sorted(results, key=lambda x: x["diem_bottleneck"], reverse=True)
    return [r for r in results_sorted if r["so_task_phu_thuoc_vao_no"] > 0][:top_n]


def find_critical_path(G: nx.DiGraph) -> list:
    """Tìm đường đi dài nhất (critical path) - chuỗi task quyết định tiến độ."""
    if not nx.is_directed_acyclic_graph(G):
        return []  # có vòng lặp -> dữ liệu phụ thuộc bị lỗi logic
    longest_path = nx.dag_longest_path(G)
    return [int(n) for n in longest_path]


if __name__ == "__main__":
    G = build_graph()
    print(f"Đồ thị: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")

    print("\n=== Top điểm nghẽn (bottleneck) ===")
    for b in detect_bottlenecks(G):
        print(b)

    print("\n=== Critical path (chuỗi task quyết định tiến độ) ===")
    print(find_critical_path(G))
