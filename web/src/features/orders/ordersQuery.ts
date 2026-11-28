import type { PaginationState, SortingState } from "@tanstack/react-table";
import type { paths } from "@/api/schema";

export type OrdersResponse = NonNullable<
    paths["/api/orders"]["get"]["responses"]["200"]["content"]
>["application/json"];
export type OrderRow = NonNullable<OrdersResponse["items"]>[number];
export type OrdersQuery = NonNullable<
    paths["/api/orders"]["get"]["parameters"]["query"]
>;

export const PAGE_SIZES = [10, 25, 50, 100];
export const DEFAULT_SORTING: SortingState = [{ id: "createdAt", desc: true }];

export type OrderFilters = {
    status?: NonNullable<OrdersQuery["Status"]>;
    from?: string;
    to?: string;
    search?: string;
    customerId?: number;
};

export const EMPTY_FILTERS: OrderFilters = {};

export function toOrdersQuery(
    pagination: PaginationState,
    sorting: SortingState,
    filters: OrderFilters,
    ): OrdersQuery {
        const search = filters.search?.trim();
        return {
            Page: pagination.pageIndex + 1,
            PageSize: pagination.pageSize,
            Sort: sorting.length
            ? sorting.map((s) => `${s.id}:${s.desc ? "desc" : "asc"}`).join(",")
            : undefined,
            Status: filters.status || undefined,
            From: filters.from || undefined,
            To: filters.to || undefined,
            Search: search || undefined,
            CustomerId: filters.customerId,
        };
}
