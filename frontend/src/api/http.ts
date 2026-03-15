const defaultApiBaseUrl = "http://localhost:5276";

const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? defaultApiBaseUrl;
const apiBaseUrl = rawApiBaseUrl.replace(/\/+$/, "");

type ApiProblem = {
  title?: string;
  detail?: string;
};

export async function requestJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as T;
}

export function getApiBaseUrl(): string {
  return apiBaseUrl;
}

async function toApiError(response: Response): Promise<Error> {
  let message = `Request failed with status ${response.status}.`;

  try {
    const problem = (await response.json()) as ApiProblem;
    if (problem.title && problem.detail) {
      message = `${problem.title} ${problem.detail}`;
    } else if (problem.title) {
      message = problem.title;
    } else if (problem.detail) {
      message = problem.detail;
    }
  } catch {
    // Response body can be empty or non-JSON for upstream failures.
  }

  return new Error(message);
}
