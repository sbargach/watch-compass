type PaginationProps = {
  label?: string;
  page: number;
  totalPages: number;
  totalResults: number;
  hasNextPage: boolean;
  disabled: boolean;
  onPrevious: () => void;
  onNext: () => void;
};

export function Pagination({
  label = "Results pagination",
  page,
  totalPages,
  totalResults,
  hasNextPage,
  disabled,
  onPrevious,
  onNext
}: PaginationProps) {
  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav className="pagination" aria-label={label}>
      <button type="button" onClick={onPrevious} disabled={disabled || page <= 1}>
        Previous
      </button>
      <p>
        Page <strong>{page}</strong> of <strong>{totalPages}</strong>
        {" | "}
        {totalResults} results
      </p>
      <button type="button" onClick={onNext} disabled={disabled || !hasNextPage}>
        Next
      </button>
    </nav>
  );
}
