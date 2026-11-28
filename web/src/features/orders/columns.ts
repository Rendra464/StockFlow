import type { ColumnDef } from "@tanstack/react-table";
import type { OrderRow } from "./ordersQuery";

export const columns: ColumnDef<OrderRow>[] = [
    { accessorKey: "orderNumber", header: "Order Number" },
    { accessorKey: "customerName", header: "Customer" },
    { accessorKey: "status", header: "Status" },
    { accessorKey: "totalAmount", header: "Total" },
    { accessorKey: "createdAt", header: "Created At" },
];
