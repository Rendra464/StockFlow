import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";
import type { OrdersQuery } from "./ordersQuery";

export function useOrders(query: OrdersQuery) {
    return useQuery({
        queryKey: ["orders", query],
        queryFn: async ({ signal }) => {
        const { data, error } = await api.GET("/api/orders", {
            params: { query },
            signal,
        });
        if (error || !data) throw new Error("Failed to load orders");
        return data;
        },
        placeholderData: keepPreviousData,
    });
}
