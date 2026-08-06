import { useState } from "react";
import axios from "axios";
import { apiClient } from "../services/apiClient";

type TestState =
  | { status: "idle"; message: string }
  | { status: "loading"; message: string }
  | { status: "success"; message: string }
  | { status: "error"; message: string };

export function ApiTestPage() {
  const [testState, setTestState] = useState<TestState>({
    status: "idle",
    message: "La conexión aún no se ha probado.",
  });

  async function testConnection() {
    setTestState({
      status: "loading",
      message: "Probando conexión...",
    });

    try {
      const response = await apiClient.get("/Cremations");

      setTestState({
        status: "success",
        message: `Conexión exitosa. Código HTTP: ${response.status}`,
      });
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const status = error.response?.status;

        if (status === 401) {
          setTestState({
            status: "success",
            message:
              "El backend respondió correctamente con 401. La conexión funciona, pero el endpoint requiere autenticación.",
          });

          return;
        }

        setTestState({
          status: "error",
          message: error.message || "No fue posible conectarse con el backend.",
        });

        return;
      }

      setTestState({
        status: "error",
        message: "Ocurrió un error desconocido.",
      });
    }
  }

  return (
    <section>
      <p className="text-sm font-medium text-slate-500">Diagnóstico</p>

      <h1 className="mt-1 text-3xl font-semibold text-slate-900">
        Conexión con la API
      </h1>

      <p className="mt-3 text-slate-600">
        Esta página verifica que React pueda comunicarse con el backend.
      </p>

      <div className="mt-8 max-w-xl rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <button
          type="button"
          onClick={testConnection}
          disabled={testState.status === "loading"}
          className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-medium text-white disabled:cursor-not-allowed disabled:opacity-50"
        >
          {testState.status === "loading" ? "Probando..." : "Probar conexión"}
        </button>

        <p className="mt-4 text-sm text-slate-600">{testState.message}</p>
      </div>
    </section>
  );
}
