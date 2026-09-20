import { useCallback, useEffect, useState } from "react";
import { getAppInfo } from "../../api/moviesApi";
import type { AppInfo } from "../../types/movies";

type State = { info: AppInfo | null; error: string | null; isLoading: boolean };

export function useAppInfo() {
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState<State>({ info: null, error: null, isLoading: true });

  useEffect(() => {
    const controller = new AbortController();
    void getAppInfo(controller.signal)
      .then((info) => {
        if (!controller.signal.aborted) setState({ info, error: null, isLoading: false });
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setState({ info: null, error: "Catalog mode could not be verified. Data may be sample data.", isLoading: false });
        }
      });
    return () => controller.abort();
  }, [attempt]);

  const retry = useCallback(() => {
    setState({ info: null, error: null, isLoading: true });
    setAttempt((current) => current + 1);
  }, []);

  return { ...state, retry };
}
