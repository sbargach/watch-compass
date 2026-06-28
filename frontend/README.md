# Watch Compass Frontend

React + TypeScript client for Watch Compass. The frontend is intentionally thin: it renders API state, validates shared inputs before requests, and keeps movie details, provider availability, pagination, and recommendation constraints visible to the user.

## Local Development

```powershell
npm install
npm run dev
```

The client defaults to `http://localhost:5276`. Override the API target when needed:

```powershell
$env:VITE_API_BASE_URL="http://localhost:5276"
```

## Quality Gates

```powershell
npm test
npm run build
```

## Implementation Notes
- `src/api` owns API URL construction and response/problem handling.
- `src/types` mirrors the backend contract shape used by the client.
- `src/components` contains reusable movie cards, poster fallback rendering, pagination, recommendations, and the details panel.
- `App.tsx` coordinates page-level state for search, genre discovery, recommendation requests, shared release-year filtering, and watch-region changes.
