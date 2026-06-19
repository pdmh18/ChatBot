"""
Phân tích đồ thị (graph) phụ thuộc công việc để phát hiện điểm nghẽn (bottleneck).

Ghi chú: Bản này dùng NetworkX + graph centrality metrics (in-degree, betweenness,
PageRank) làm baseline — đây là cách tiếp cận "graph analysis" cổ điển.

Khi có đủ dữ liệu lịch sử (nhiều dự án, nhãn "đã từng là bottleneck"), có thể nâng
cấp lên GNN thực sự (PyTorch Geometric / DGL) để học biểu diễn (embedding) node
và dự đoán xác suất bottleneck thay vì chỉ dùng centrality.

Chạy: python -m src.models.bottleneck_detector
"""
import pandas as pd
import networkx as nx
from pathlib import Path

PROCESSED_DIR = Path(__file__).resolve().parent.parent.parent / "data" / "processed"


def build_graph() -> nx.DiGraph:
    """Xây đồ thị có hướng: task_truoc -> task_sau."""
    phu_thuoc_df = pd.read_csv(PROCESSED_DIR / "phu_thuoc_clean.csv")
    cong_viec_df = pd.read_csv(PROCESSED_DIR / "cong_viec_clean.csv")

    G = nx.DiGraph()
    for _, row in cong_viec_df.iterrows():
        G.add_node(row["task_id"], story_point=row["story_point"], do_phuc_tap=row["do_phuc_tap"])

    for _, row in phu_thuoc_df.iterrows():
        if row["task_truoc"] in G.nodes and row["task_sau"] in G.nodes:
            G.add_edge(row["task_truoc"], row["task_sau"])

    return G


def detect_bottlenecks(G: nx.DiGraph, top_n: int = 5) -> list:
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

    results = []
    for node in G.nodes:
        bottleneck_score = (
            0.4 * (out_degree.get(node, 0) / max(out_degree.values(), default=1))
            + 0.35 * betweenness.get(node, 0)
            + 0.25 * pagerank.get(node, 0)
        )
        results.append({
            "task_id": node,
            "so_task_phu_thuoc_vao_no": out_degree.get(node, 0),
            "diem_trung_tam": round(betweenness.get(node, 0), 4),
            "diem_bottleneck": round(bottleneck_score * 100, 2),
        })

    results_sorted = sorted(results, key=lambda x: x["diem_bottleneck"], reverse=True)
    return [r for r in results_sorted if r["so_task_phu_thuoc_vao_no"] > 0][:top_n]


def find_critical_path(G: nx.DiGraph) -> list:
    """Tìm đường đi dài nhất (critical path) trong đồ thị — chuỗi task quyết định tiến độ."""
    if not nx.is_directed_acyclic_graph(G):
        return []  # có vòng lặp -> dữ liệu phụ thuộc bị lỗi logic

    longest_path = nx.dag_longest_path(G)
    return longest_path


if __name__ == "__main__":
    G = build_graph()
    print(f"Đồ thị: {G.number_of_nodes()} nodes, {G.number_of_edges()} edges")

    print("\n=== Top điểm nghẽn (bottleneck) ===")
    for b in detect_bottlenecks(G):
        print(b)

    print("\n=== Critical path (chuỗi task quyết định tiến độ) ===")
    print(find_critical_path(G))
