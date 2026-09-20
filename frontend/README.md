# Watch Compass Frontend

React and TypeScript client for Watch Compass. Catalog requests go through the real ASP.NET Core API in both demo and live modes.

## Local development

Start the backend from the repository root:

```sh
dotnet run --project src/WatchCompass.Api --launch-profile Demo
```

Then in this directory:

```sh
npm ci
npm run dev
```

Open [localhost:5173](http://localhost:5173). The client defaults to `http://localhost:5276`; set `VITE_API_BASE_URL` before starting Vite to change it.

The backend's `/api/app-info` response controls the demo notice, feed labels, provider wording, and credits. There is no separate frontend demo flag. If that request fails, the UI displays uncertainty and a retry action rather than assuming live mode. Reload the page after restarting the backend in a different mode.

Demo SVG posters live in `public/demo/posters`. The demo uses no remote images, fonts, or API services after dependencies are installed. See the [root README](../README.md) for the walkthrough and live TMDB setup.

## Quality gates

```sh
npm run lint
npm test
npm run build
```

## Implementation notes

- `src/api` owns API URLs and response/problem handling.
- `src/types` mirrors backend request and response contracts.
- `src/features` owns abortable catalog, browsing, recommendation, and details state.
- `src/components` contains movie cards, pagination, the compass picker, and the details drawer.
- `App.tsx` coordinates shared region and year inputs and composes the page.

The optional recommendation hint searches movie titles. Mood is a required genre rule, including when a hint is supplied.

## Credits

Demo content and artwork are fictional examples. Live mode uses TMDB data and artwork and displays its attribution.
