import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { usePagedMovies } from "./usePagedMovies";

describe("usePagedMovies", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("aborts the obsolete request and keeps the latest response", async () => {
    const capture: { firstSignal: AbortSignal | null } = { firstSignal: null };
    const fetchMock = vi.fn(async (input: string | URL | Request, init?: RequestInit) => {
      const url = String(input);
      if (url.includes("Arrival")) {
        capture.firstSignal = init?.signal ?? null;
        await new Promise((resolve) => setTimeout(resolve, 40));
        return jsonResponse(resultWithTotal(99));
      }
      return jsonResponse(resultWithTotal(1));
    });
    vi.stubGlobal("fetch", fetchMock);

    const { result, rerender } = renderHook(
      ({ value }) => usePagedMovies({ mode: "search", value, page: 1, pageSize: 12, isValid: true }),
      { initialProps: { value: "Arrival" } }
    );

    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    rerender({ value: "Dune" });
    await waitFor(() => expect(result.current.result?.totalResults).toBe(1));
    expect(capture.firstSignal?.aborted).toBe(true);

    await new Promise((resolve) => setTimeout(resolve, 60));
    expect(result.current.result?.totalResults).toBe(1);
  });
});

function resultWithTotal(totalResults: number) {
  return { items: [], page: 1, pageSize: 12, totalResults, totalPages: totalResults, hasNextPage: false };
}

function jsonResponse(body: unknown): Response {
  return { ok: true, json: async () => body } as Response;
}
