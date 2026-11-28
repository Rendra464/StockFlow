import { Search, CalendarDays, ChevronDown, X } from "lucide-react";
import type { OrderFilters } from "./ordersQuery";

type Props = {
  filters: OrderFilters;
  onChange: (next: OrderFilters) => void;
};

const STATUSES = [
  "Pending",
  "Paid",
  "Processing",
  "Shipped",
  "Completed",
  "Cancelled",
  "Expired",
] as const;

export function OrdersFilters({ filters, onChange }: Props) {
  const hasFilters = Boolean(
    filters.status || filters.from || filters.to || filters.search,
  );

  const clearFilters = () => {
    onChange({
      ...filters,
      status: undefined,
      from: undefined,
      to: undefined,
      search: undefined,
    });
  };

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
        <div className="relative min-w-0 flex-1">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-400" />

          <input
            type="text"
            placeholder="Search orders or customers..."
            value={filters.search ?? ""}
            onChange={(e) =>
              onChange({
                ...filters,
                search: e.target.value || undefined,
              })
            }
            className="h-9 w-full rounded-md border border-zinc-200 bg-white pl-9 pr-3 text-sm text-zinc-900 outline-none transition-colors placeholder:text-zinc-400 hover:border-zinc-300 focus:border-zinc-400 focus:ring-2 focus:ring-zinc-100 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-100 dark:hover:border-zinc-700 dark:focus:border-zinc-600 dark:focus:ring-zinc-800"
          />
        </div>

        <div className="flex flex-col gap-2 sm:flex-row sm:flex-wrap">
          <div className="relative">
            <select
              value={filters.status ?? ""}
              onChange={(e) =>
                onChange({
                  ...filters,
                  status: (e.target.value ||
                    undefined) as OrderFilters["status"],
                })
              }
              className="h-9 w-full appearance-none rounded-md border border-zinc-200 bg-white py-1.5 pl-3 pr-9 text-sm text-zinc-700 outline-none transition-colors hover:bg-zinc-50 focus:border-zinc-400 focus:ring-2 focus:ring-zinc-100 sm:w-40 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300 dark:hover:bg-zinc-900 dark:focus:border-zinc-600 dark:focus:ring-zinc-800"
            >
              <option value="">All statuses</option>
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>

            <ChevronDown className="pointer-events-none absolute right-3 top-1/2 size-3.5 -translate-y-1/2 text-zinc-400" />
          </div>

          <div className="flex items-center gap-2">
            <div className="relative min-w-0 flex-1">
              <CalendarDays className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-400" />

              <input
                type="date"
                aria-label="From date"
                value={filters.from ?? ""}
                max={filters.to}
                onChange={(e) =>
                  onChange({
                    ...filters,
                    from: e.target.value || undefined,
                  })
                }
                className="h-9 w-full min-w-0 rounded-md border border-zinc-200 bg-white pl-9 pr-2 text-sm text-zinc-700 outline-none transition-colors hover:bg-zinc-50 focus:border-zinc-400 focus:ring-2 focus:ring-zinc-100 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300 dark:hover:bg-zinc-900 dark:focus:border-zinc-600 dark:focus:ring-zinc-800"
              />
            </div>

            <span className="text-xs text-zinc-400">to</span>

            <div className="relative min-w-0 flex-1">
              <CalendarDays className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-zinc-400" />

              <input
                type="date"
                aria-label="To date"
                value={filters.to ?? ""}
                min={filters.from}
                onChange={(e) =>
                  onChange({
                    ...filters,
                    to: e.target.value || undefined,
                  })
                }
                className="h-9 w-full min-w-0 rounded-md border border-zinc-200 bg-white pl-9 pr-2 text-sm text-zinc-700 outline-none transition-colors hover:bg-zinc-50 focus:border-zinc-400 focus:ring-2 focus:ring-zinc-100 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300 dark:hover:bg-zinc-900 dark:focus:border-zinc-600 dark:focus:ring-zinc-800"
              />
            </div>
          </div>
        </div>
      </div>

      {hasFilters && (
        <div className="flex flex-wrap items-center justify-between gap-2 border-t border-zinc-100 pt-3 dark:border-zinc-800">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-xs text-zinc-500">Active filters:</span>

            {filters.status && (
              <span className="inline-flex items-center rounded-md border border-zinc-200 bg-zinc-50 px-2 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
                {filters.status}
              </span>
            )}

            {filters.from && (
              <span className="inline-flex items-center rounded-md border border-zinc-200 bg-zinc-50 px-2 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
                From: {filters.from}
              </span>
            )}

            {filters.to && (
              <span className="inline-flex items-center rounded-md border border-zinc-200 bg-zinc-50 px-2 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
                To: {filters.to}
              </span>
            )}

            {filters.search && (
              <span className="inline-flex max-w-full items-center rounded-md border border-zinc-200 bg-zinc-50 px-2 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
                <span className="truncate">Search: {filters.search}</span>
              </span>
            )}
          </div>

          <button
            type="button"
            onClick={clearFilters}
            className="inline-flex shrink-0 items-center gap-1 rounded-md px-2 py-1 text-xs font-medium text-zinc-500 transition-colors hover:bg-zinc-100 hover:text-zinc-900 dark:hover:bg-zinc-900 dark:hover:text-zinc-100"
          >
            <X className="size-3.5" />
            Clear filters
          </button>
        </div>
      )}
    </div>
  );
}
