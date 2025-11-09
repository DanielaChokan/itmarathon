import { useEffect, useState, useRef } from "react";
import type { FetchParams, UseFetchReturn } from "./types";

export function useFetch<T = unknown, N = undefined>(
  { url, onSuccess, onError, ...options }: FetchParams<T>,
  immediate: boolean = true,
): UseFetchReturn<T, N> {
  const [data, setData] = useState<T | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isError, setIsError] = useState<boolean>(false);

  const abortControllerRef = useRef<AbortController | null>(null);

  const fetchData = async (data?: N, overrideOptions?: Partial<FetchParams<T>>) => {
    abortControllerRef.current?.abort();
    const controller = new AbortController();
    abortControllerRef.current = controller;

    setIsLoading(true);
    setIsError(false);

    // Використовуємо overrideOptions якщо вони передані
    const fetchUrl = overrideOptions?.url || url;
    const fetchOptions = {
      ...options,
      ...overrideOptions,
      body: data ? JSON.stringify(data) : undefined,
      signal: controller.signal,
    };

    try {
      const response = await fetch(fetchUrl, fetchOptions);

      if (!response.ok) {
        throw new Error(`HTTP error! Status: ${response.status}`);
      }

      // Перевіряємо чи є контент у відповіді (для DELETE може бути порожньо)
      const contentType = response.headers.get("content-type");
      const result: T = contentType?.includes("application/json")
        ? await response.json()
        : (null as T);
      
      setData(result);

      if (onSuccess) {
        onSuccess(result);
      }
    } catch (error) {
      if ((error as Error).name !== "AbortError") {
        setIsError(true);
      }

      if (onError) {
        onError(error as Error);
      }
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (immediate) {
      fetchData();
    }

    return () => {
      abortControllerRef.current?.abort();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [immediate]);

  return { data, isLoading, isError, fetchData };
}
