import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import App from "../../App";

const sampleMovie = {
  movieId: 1001, title: "The Last Stroopwafel", runtimeMinutes: 94, genres: ["Comedy"],
  posterUrl: "/demo/posters/1001.svg", backdropUrl: null, releaseYear: 2024, overview: "A fictional film."
};
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), {
  status, headers: { "Content-Type": "application/json" }
});

function mockApi(appInfo: () => Response) {
  const fetchMock = vi.fn<typeof fetch>(async (input) => {
    const url = String(input);
    if (url.endsWith("/api/app-info")) return appInfo();
    if (url.endsWith("/api/genres")) return json({ items: ["Comedy"] });
    if (url.includes("/api/movies/1001?")) return json({ ...sampleMovie, providers: ["Canal Cinema (sample)"] });
    if (url.endsWith("/similar")) return json({ items: [] });
    if (url.includes("/trending?") || url.includes("/now-playing?")) return json({ items: [sampleMovie] });
    throw new Error("Unexpected request: " + url);
  });
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

describe("catalog identity", () => {
  it("labels fictional data throughout the demo and uses only local artwork", async () => {
    mockApi(() => json({ catalogMode: "demo" }));
    const user = userEvent.setup();
    const { container } = render(<App />);
    expect(await screen.findByLabelText("Demo catalog")).toHaveTextContent("fictional films");
    expect(screen.getByRole("heading", { name: "Sample Now Playing" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Sample Trending" })).toBeInTheDocument();
    expect(screen.getByLabelText("Optional title hint")).toHaveAttribute("placeholder", "Movie title or part of a title...");
    await user.click(screen.getAllByRole("button", { name: /The Last Stroopwafel/ })[0]);
    expect(await screen.findByText("Simulated availability in Netherlands (NL)")).toBeInTheDocument();
    expect(screen.getByText("Canal Cinema (sample)")).toBeInTheDocument();
    for (const image of container.querySelectorAll("img")) {
      expect(image.getAttribute("src")).toMatch(/^\/(?!\/)/);
    }
    expect(screen.queryByAltText("The Movie Database (TMDB)")).not.toBeInTheDocument();
  });

  it("keeps TMDB attribution for live mode", async () => {
    mockApi(() => json({ catalogMode: "tmdb" }));
    render(<App />);
    expect(await screen.findByAltText("The Movie Database (TMDB)")).toBeInTheDocument();
    expect(screen.queryByLabelText("Demo catalog")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Trending Today" })).toBeInTheDocument();
  });

  it("shows uncertainty after a metadata failure and can retry", async () => {
    let attempts = 0;
    mockApi(() => ++attempts === 1 ? json({ title: "Unavailable" }, 503) : json({ catalogMode: "demo" }));
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("alert")).toHaveTextContent("Data may be sample data");
    expect(screen.queryByAltText("The Movie Database (TMDB)")).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Retry catalog status" }));
    expect(await screen.findByLabelText("Demo catalog")).toBeInTheDocument();
    expect(attempts).toBe(2);
  });

  it("does not treat an unknown metadata value as live mode", async () => {
    mockApi(() => json({ catalogMode: "unexpected" }));
    render(<App />);
    expect(await screen.findByRole("alert")).toHaveTextContent("could not be verified");
    expect(screen.queryByAltText("The Movie Database (TMDB)")).not.toBeInTheDocument();
  });
});
