import { flexRender, type Table } from "@tanstack/react-table";
import { AlertCircle, ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import type { OrderRow } from "./ordersQuery";

type Props = {
  table: Table<OrderRow>;
  isLoading: boolean;
  isError: boolean;
};

export function OrdersTable({ table, isLoading, isError }: Props) {
  const headerGroups = table.getHeaderGroups();
  const rows = table.getRowModel().rows;

  const columnCount = Math.max(headerGroups[0]?.headers.length ?? 0, 1);

  const pageIndex = table.getState().pagination.pageIndex;
  const pageCount = table.getPageCount();

  const getPageLabel = () => {
    if (pageCount === 0) return "Page 0 of 0";

    return `Page ${pageIndex + 1} of ${pageCount}`;
  };

  if (isError) {
    return (
      <div
        role="alert"
        className="flex items-start gap-3 rounded-lg border border-red-200 bg-red-50/50 px-4 py-4 text-sm text-red-700 dark:border-red-900/60 dark:bg-red-950/20 dark:text-red-300"
      >
        <AlertCircle className="mt-0.5 size-4 shrink-0" />

        <div>
          <p className="font-medium">Failed to load orders</p>
          <p className="mt-1 text-red-600/80 dark:text-red-400/80">
            We couldn't retrieve your orders. Please try again.
          </p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
        <div className="overflow-x-auto">
          <table className="w-full min-w-[640px] text-left">
            <thead className="bg-zinc-50/80 dark:bg-zinc-900/70">
              <tr>
                {headerGroups[0]?.headers.map((h) => (
                  <th
                    key={h.id}
                    className="px-4 py-3 text-xs font-medium uppercase tracking-wider text-zinc-500"
                  >
                    {flexRender(h.column.columnDef.header, h.getContext())}
                  </th>
                ))}
              </tr>
            </thead>

            <tbody className="divide-y divide-zinc-200 dark:divide-zinc-800">
              {Array.from({ length: 5 }).map((_, index) => (
                <tr key={index} className="animate-pulse">
                  {Array.from({ length: columnCount }).map((_, cellIndex) => (
                    <td key={cellIndex} className="px-4 py-4">
                      <div className="h-4 w-full max-w-36 rounded bg-zinc-200/70 dark:bg-zinc-800" />
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="flex items-center justify-between border-t border-zinc-200 px-4 py-3 dark:border-zinc-800">
          <div className="h-4 w-24 animate-pulse rounded bg-zinc-200/70 dark:bg-zinc-800" />

          <div className="flex gap-2">
            <div className="h-8 w-20 animate-pulse rounded-md bg-zinc-200/70 dark:bg-zinc-800" />
            <div className="h-8 w-16 animate-pulse rounded-md bg-zinc-200/70 dark:bg-zinc-800" />
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-950">
      <div className="overflow-x-auto">
        <table className="w-full min-w-[640px] text-left">
          <thead className="bg-zinc-50/80 dark:bg-zinc-900/70">
            {headerGroups.map((hg) => (
              <tr key={hg.id}>
                {hg.headers.map((h) => {
                  const sorted = h.column.getIsSorted();
                  const canSort = h.column.getCanSort();

                  return (
                    <th
                      key={h.id}
                      scope="col"
                      colSpan={h.colSpan}
                      onClick={
                        canSort ? h.column.getToggleSortingHandler() : undefined
                      }
                      className={[
                        "px-4 py-3 text-xs font-medium uppercase tracking-wider text-zinc-500",
                        canSort
                          ? "cursor-pointer select-none hover:text-zinc-900 dark:hover:text-zinc-100"
                          : "",
                      ].join(" ")}
                      aria-sort={
                        sorted === "asc"
                          ? "ascending"
                          : sorted === "desc"
                            ? "descending"
                            : "none"
                      }
                    >
                      {h.isPlaceholder ? null : (
                        <div className="flex items-center gap-2">
                          <span>
                            {flexRender(
                              h.column.columnDef.header,
                              h.getContext(),
                            )}
                          </span>

                          {canSort && (
                            <span className="text-zinc-400">
                              {sorted === "asc" ? (
                                <ArrowUp className="size-3.5" />
                              ) : sorted === "desc" ? (
                                <ArrowDown className="size-3.5" />
                              ) : (
                                <ArrowUpDown className="size-3.5 opacity-50" />
                              )}
                            </span>
                          )}
                        </div>
                      )}
                    </th>
                  );
                })}
              </tr>
            ))}
          </thead>

          <tbody className="divide-y divide-zinc-200 dark:divide-zinc-800">
            {rows.length === 0 ? (
              <tr>
                <td colSpan={columnCount} className="px-4 py-16 text-center">
                  <div className="mx-auto flex max-w-sm flex-col items-center">
                    <div className="flex size-10 items-center justify-center rounded-full bg-zinc-100 dark:bg-zinc-900">
                      <AlertCircle className="size-5 text-zinc-400" />
                    </div>

                    <p className="mt-3 text-sm font-medium text-zinc-900 dark:text-zinc-100">
                      No orders found
                    </p>

                    <p className="mt-1 text-sm text-zinc-500">
                      There are no orders to display right now.
                    </p>
                  </div>
                </td>
              </tr>
            ) : (
              rows.map((row) => (
                <tr
                  key={row.id}
                  className="transition-colors hover:bg-zinc-50/90 dark:hover:bg-zinc-900/50"
                >
                  {row.getVisibleCells().map((c) => (
                    <td
                      key={c.id}
                      className="px-4 py-3 text-sm text-zinc-700 dark:text-zinc-300"
                    >
                      {flexRender(c.column.columnDef.cell, c.getContext())}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-3 border-t border-zinc-200 px-4 py-3 sm:flex-row sm:items-center sm:justify-between dark:border-zinc-800">
        <span className="text-xs tabular-nums text-zinc-500">
          {getPageLabel()}
        </span>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
            className="rounded-md border border-zinc-200 bg-white px-3 py-1.5 text-xs font-medium text-zinc-700 transition-colors hover:bg-zinc-50 disabled:pointer-events-none disabled:opacity-40 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300 dark:hover:bg-zinc-900"
          >
            Previous
          </button>

          <button
            type="button"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
            className="rounded-md border border-zinc-200 bg-white px-3 py-1.5 text-xs font-medium text-zinc-700 transition-colors hover:bg-zinc-50 disabled:pointer-events-none disabled:opacity-40 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-300 dark:hover:bg-zinc-900"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}
