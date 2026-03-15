type PaginationProps = {
  page: number;
  totalPages: number;
  totalResults: number;
  hasNextPage: boolean;
  disabled: boolean;
  onPrevious: () => void;
  onNext: () => void;
};

export function Pagination({
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
    <nav className="pagination" aria-label="Search pagination">
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
