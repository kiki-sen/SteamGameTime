export interface PageResultDto<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
  sort?: string;
  q?: string;
}