const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

/**
 * Register new RSGM user.
 */
export async function registerUser(registerData) {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/register`,
    {
      method: "POST",

      headers: {
        "Content-Type": "application/json",
      },

      body: JSON.stringify(registerData),
    }
  );

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      getErrorMessage(data, "Registration failed.")
    );
  }

  return data;
}

/**
 * Login existing RSGM user.
 */
export async function loginUser(loginData) {
  const response = await fetch(
    `${API_BASE_URL}/api/auth/login`,
    {
      method: "POST",

      headers: {
        "Content-Type": "application/json",
      },

      body: JSON.stringify(loginData),
    }
  );

  const data = await readResponse(response);

  if (!response.ok) {
    throw new Error(
      getErrorMessage(
        data,
        "Invalid email or password."
      )
    );
  }

  return data;
}

/**
 * Save JWT authentication information.
 */
export function saveAuth(data, rememberMe = false) {
  const token =
    data.token ??
    data.accessToken ??
    data.jwtToken;

  if (!token) {
    throw new Error(
      "The backend did not return a JWT token."
    );
  }

  const storage = rememberMe
    ? localStorage
    : sessionStorage;

  // Clear any previous login first.
  localStorage.removeItem("rsgm_token");
  localStorage.removeItem("rsgm_role");
  localStorage.removeItem("rsgm_user");

  sessionStorage.removeItem("rsgm_token");
  sessionStorage.removeItem("rsgm_role");
  sessionStorage.removeItem("rsgm_user");

  storage.setItem(
    "rsgm_token",
    token
  );

  const role =
    data.role ??
    data.roles?.[0] ??
    data.user?.role ??
    "";

  if (role) {
    storage.setItem(
      "rsgm_role",
      role
    );
  }

  const user =
    data.user ?? {
      email: data.email,
      role,
    };

  storage.setItem(
    "rsgm_user",
    JSON.stringify(user)
  );
}

/**
 * Get current JWT token.
 */
export function getToken() {
  return (
    localStorage.getItem("rsgm_token") ||
    sessionStorage.getItem("rsgm_token")
  );
}

/**
 * Get current user role.
 */
export function getRole() {
  return (
    localStorage.getItem("rsgm_role") ||
    sessionStorage.getItem("rsgm_role")
  );
}

/**
 * Get current logged-in user.
 */
export function getCurrentUser() {
  const storedUser =
    localStorage.getItem("rsgm_user") ||
    sessionStorage.getItem("rsgm_user");

  if (!storedUser) {
    return null;
  }

  try {
    return JSON.parse(storedUser);
  } catch {
    return null;
  }
}

/**
 * Check authentication.
 */
export function isAuthenticated() {
  return Boolean(getToken());
}

/**
 * Logout.
 */
export function logout() {
  localStorage.removeItem("rsgm_token");
  localStorage.removeItem("rsgm_role");
  localStorage.removeItem("rsgm_user");

  sessionStorage.removeItem("rsgm_token");
  sessionStorage.removeItem("rsgm_role");
  sessionStorage.removeItem("rsgm_user");
}

/**
 * Authorization header for protected API requests.
 */
export function getAuthHeaders() {
  const token = getToken();

  return {
    "Content-Type": "application/json",

    ...(token && {
      Authorization: `Bearer ${token}`,
    }),
  };
}

async function readResponse(response) {
  const text = await response.text();

  if (!text) {
    return {};
  }

  try {
    return JSON.parse(text);
  } catch {
    return {
      message: text,
    };
  }
}

function getErrorMessage(data, fallback) {
  if (!data) {
    return fallback;
  }

  if (typeof data === "string") {
    return data;
  }

  if (
    Array.isArray(data.errors) &&
    data.errors.length > 0
  ) {
    return data.errors.join(" ");
  }

  if (
    data.errors &&
    typeof data.errors === "object"
  ) {
    return Object.values(data.errors)
      .flat()
      .join(" ");
  }

  if (data.message) {
    return data.message;
  }

  if (data.error) {
    return data.error;
  }

  return fallback;
}