"""
Entry point FastAPI server.
Chạy: uvicorn src.api.main:app --reload --port 8000
"""
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from src.api.routes import risk, chat

app = FastAPI(
    title="ChatBox AI Server",
    description="API dự báo rủi ro & tối ưu nguồn lực dự án phần mềm",
    version="1.0.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # sửa lại domain cụ thể khi deploy production
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(risk.router)
app.include_router(chat.router)


@app.get("/health", tags=["Health"])
def health_check():
    return {"status": "ok"}


@app.get("/", tags=["Health"])
def root():
    return {"message": "ChatBox AI Server đang chạy. Xem docs tại /docs"}
