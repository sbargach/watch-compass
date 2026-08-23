import { useEffect, useRef, useState, type FormEvent } from "react";
import { getRecommendations } from "../../api/moviesApi";
import type { Mood, Recommendation, RecommendationsRequest } from "../../types/movies";

export type RecommendationFormState = {
  mood: Mood;
  timeBudgetMinutes: string;
  query: string;
  avoidGenres: string[];
};

type RecommendationState = {
  items: Recommendation[];
  isLoading: boolean;
  error: string | null;
  hasRequested: boolean;
  appliedRequestKey: string | null;
};

type Options = {
  watchRegion: string;
  releaseYearInput: string;
  releaseYear: number | null;
  releaseYearValidationMessage: string | null;
  onBeforeRequest: () => void;
};

const initialForm: RecommendationFormState = {
  mood: "FeelGood",
  timeBudgetMinutes: "120",
  query: "",
  avoidGenres: []
};

const initialState: RecommendationState = {
  items: [],
  isLoading: false,
  error: null,
  hasRequested: false,
  appliedRequestKey: null
};

export function useRecommendations({
  watchRegion,
  releaseYearInput,
  releaseYear,
  releaseYearValidationMessage,
  onBeforeRequest
}: Options) {
  const [form, setForm] = useState<RecommendationFormState>(initialForm);
  const [state, setState] = useState<RecommendationState>(initialState);
  const controllerRef = useRef<AbortController | null>(null);
  const requestKey = buildRequestKey(form, watchRegion, releaseYearInput, releaseYear, releaseYearValidationMessage);

  useEffect(() => () => controllerRef.current?.abort(), [requestKey]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const parsedTimeBudget = Number.parseInt(form.timeBudgetMinutes, 10);
    if (Number.isNaN(parsedTimeBudget) || parsedTimeBudget < 1 || parsedTimeBudget > 600) {
      setState(errorState("Time budget must be between 1 and 600 minutes.", requestKey));
      return;
    }
    if (releaseYearValidationMessage !== null) {
      setState(errorState(releaseYearValidationMessage, requestKey));
      return;
    }

    const request: RecommendationsRequest = {
      mood: form.mood,
      timeBudgetMinutes: parsedTimeBudget,
      query: form.query.trim() || undefined,
      avoidGenres: form.avoidGenres,
      releaseYear: releaseYear ?? undefined,
      countryCode: watchRegion
    };

    onBeforeRequest();
    setState({ items: [], isLoading: true, error: null, hasRequested: true, appliedRequestKey: requestKey });
    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;

    try {
      const response = await getRecommendations(request, controller.signal);
      if (!controller.signal.aborted) {
        setState({
          items: response.items,
          isLoading: false,
          error: null,
          hasRequested: true,
          appliedRequestKey: requestKey
        });
      }
    } catch (error) {
      if (!controller.signal.aborted) {
        setState(errorState(toErrorMessage(error), requestKey));
      }
    }
  };

  return {
    recommendationForm: form,
    setRecommendationForm: setForm,
    recommendationState: state,
    recommendationStateMatchesRequest: state.appliedRequestKey === requestKey,
    handleRecommendationSubmit: submit
  };
}

function errorState(error: string, requestKey: string): RecommendationState {
  return { items: [], isLoading: false, error, hasRequested: true, appliedRequestKey: requestKey };
}

function toErrorMessage(error: unknown): string {
  return error instanceof Error && error.message.trim().length > 0
    ? error.message
    : "Unexpected error while calling the API.";
}

function buildRequestKey(
  form: RecommendationFormState,
  watchRegion: string,
  releaseYearInput: string,
  releaseYear: number | null,
  validationMessage: string | null
): string {
  const normalizedAvoidGenres = [...form.avoidGenres]
    .map((genre) => genre.trim())
    .filter((genre) => genre.length > 0)
    .sort((left, right) => left.localeCompare(right));
  const releaseYearKey = validationMessage === null
    ? `valid:${releaseYear ?? "all"}`
    : `invalid:${releaseYearInput.trim()}`;
  return [
    `mood:${form.mood}`,
    `budget:${form.timeBudgetMinutes.trim()}`,
    `query:${form.query.trim()}`,
    `avoid:${normalizedAvoidGenres.join(",")}`,
    `country:${watchRegion}`,
    `releaseYear:${releaseYearKey}`
  ].join("|");
}
