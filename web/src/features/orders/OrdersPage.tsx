import { useState } from "react";
import {
    getCoreRowModel,
    useReactTable,
    type PaginationState,
    type SortingState,
} from "@tanstack/react-table";
import { columns } from "./columns";
import {
    DEFAULT_SORTING,
    EMPTY_FILTERS,
    toOrdersQuery,
    type OrderFilters,
} from "./ordersQuery";
import { useOrders } from "./useOrders";
import { OrdersTable } from "./OrdersTable";
import { OrdersFilters } from "./OrdersFilters";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";

export default function OrdersPage() {
    const [pagination, setPagination] = useState<PaginationState>({
        pageIndex: 0,
        pageSize: 25,
    });
    const [sorting, setSorting] = useState<SortingState>(DEFAULT_SORTING);
    const [filters, setFilters] = useState<OrderFilters>(EMPTY_FILTERS);
    
    const debouncedSearch = useDebouncedValue(filters.search, 300);

    const { data, isLoading, isError } = useOrders(
        toOrdersQuery(pagination, sorting, { ...filters, search: debouncedSearch }),
    );

    const table = useReactTable({
        data: data?.items ?? [],
        columns,
        rowCount: data?.totalCount !== undefined ? Number(data.totalCount) : 0,
        state: { pagination, sorting },
        manualPagination: true,
        manualSorting: true,
        getCoreRowModel: getCoreRowModel(),
        onSortingChange: (updater) => {
            setSorting(updater);
            setPagination((p) => ({ ...p, pageIndex: 0 }));
        },
        onPaginationChange: (updater) =>
            setPagination((old) => {
                const next = typeof updater === "function" ? updater(old) : updater;
                return next.pageSize !== old.pageSize ? { ...next, pageIndex: 0 } : next;
            }),
    });

    const handleFiltersChange = (next: OrderFilters) => {
        setFilters(next);
        setPagination((p) => ({ ...p, pageIndex: 0 }));
    };

    return (
        <>
            <OrdersFilters filters={filters} onChange={handleFiltersChange} />
            <OrdersTable table={table} isLoading={isLoading} isError={isError} />
        </>
    );
}
