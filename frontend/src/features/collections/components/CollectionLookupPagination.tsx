interface CollectionLookupPaginationProps {
  page: number;
  totalPages: number;
  isFetching: boolean;
  onPageChange: (page: number) => void;
}

export function CollectionLookupPagination({
  page,
  totalPages,
  isFetching,
  onPageChange,
}: CollectionLookupPaginationProps) {
  if (totalPages <= 1) {
    return null;
  }

  return (
    <div className="mt-2 flex items-center justify-between gap-2 text-xs text-slate-500">
      <button
        type="button"
        onClick={() => onPageChange(page - 1)}
        disabled={page <= 1 || isFetching}
        className="rounded border border-slate-300 px-2 py-1 disabled:opacity-50"
      >
        Anterior
      </button>

      <span>
        Página {page} de {totalPages}
      </span>

      <button
        type="button"
        onClick={() => onPageChange(page + 1)}
        disabled={page >= totalPages || isFetching}
        className="rounded border border-slate-300 px-2 py-1 disabled:opacity-50"
      >
        Siguiente
      </button>
    </div>
  );
}
