export interface LookupItemDto {
  id: number;
  name: string;
}

export interface UserLookupDto {
  id: number;
  hoTen: string;
  email: string;
  vaiTro: string;
}

export interface SprintLookupDto {
  id: number;
  tenSprint: string;
  maDuAn: number;
  tenDuAn: string;
}
