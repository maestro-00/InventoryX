// Example React component for authentication with Google OAuth and traditional login
// This demonstrates how to integrate with the InventoryX API

import React, { useState } from 'react';

interface LoginFormProps {
  apiBaseUrl: string; // e.g., "http://localhost:5000"
}

export const LoginForm: React.FC<LoginFormProps> = ({ apiBaseUrl }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Traditional password login
  const handlePasswordLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/login?useCookies=true`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include', // Important: Include cookies
        body: JSON.stringify({ email, password }),
      });

      if (response.ok) {
        // Login successful - redirect to dashboard
        window.location.href = '/dashboard';
      } else {
        const errorData = await response.json();
        setError(errorData.title || 'Login failed. Please check your credentials.');
      }
    } catch (err) {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  // Google OAuth login
  const handleGoogleLogin = () => {
    // Get the current origin for the return URL
    const returnUrl = encodeURIComponent(`${window.location.origin}/auth/callback`);
    
    // IMPORTANT: Use window.location.href for OAuth - NOT fetch()!
    // OAuth requires a full browser navigation, not an AJAX request
    // Flow: SPA → Backend /external-login → Google OAuth → Backend /google-callback → SPA /auth/callback
    window.location.href = `${apiBaseUrl}/api/auth/external-login?provider=Google&returnUrl=${returnUrl}`;
  };

  return (
    <div className="login-container">
      <h2>Sign In</h2>
      
      {error && (
        <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>
          {error}
        </div>
      )}

      {/* Traditional Login Form */}
      <form onSubmit={handlePasswordLogin}>
        <div className="form-group">
          <label htmlFor="email">Email</label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            disabled={loading}
          />
        </div>

        <div className="form-group">
          <label htmlFor="password">Password</label>
          <input
            type="password"
            id="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            disabled={loading}
          />
        </div>

        <button type="submit" disabled={loading}>
          {loading ? 'Signing in...' : 'Sign In with Password'}
        </button>
      </form>

      {/* Divider */}
      <div className="divider" style={{ margin: '1.5rem 0', textAlign: 'center' }}>
        <span>OR</span>
      </div>

      {/* Google Login Button */}
      <button
        type="button"
        onClick={handleGoogleLogin}
        className="google-login-btn"
        style={{
          width: '100%',
          padding: '0.75rem',
          backgroundColor: '#4285f4',
          color: 'white',
          border: 'none',
          borderRadius: '4px',
          cursor: 'pointer',
        }}
      >
        <span>🔐 Sign In with Google</span>
      </button>

      {/* Register Link */}
      <div style={{ marginTop: '1rem', textAlign: 'center' }}>
        <a href="/register">Don't have an account? Register here</a>
      </div>
    </div>
  );
};

// Callback handler component
// This should be rendered at the route: /auth/callback
// The backend redirects here AFTER completing Google OAuth and setting the auth cookie
export const AuthCallback: React.FC<{ apiBaseUrl: string }> = ({ apiBaseUrl }) => {
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');

  React.useEffect(() => {
    const verifyAuth = async () => {
      try {
        // The backend has already authenticated the user and set the cookie
        // We just need to verify it worked
        const response = await fetch(`${apiBaseUrl}/api/auth/pingauth`, {
          credentials: 'include',
        });

        if (response.ok) {
          const data = await response.json();
          console.log('Authenticated as:', data.email);
          setStatus('success');
          
          // Redirect to dashboard after a brief delay
          setTimeout(() => {
            window.location.href = '/dashboard';
          }, 1000);
        } else {
          setStatus('error');
        }
      } catch (err) {
        console.error('Auth verification failed:', err);
        setStatus('error');
      }
    };

    verifyAuth();
  }, [apiBaseUrl]);

  return (
    <div className="auth-callback" style={{ textAlign: 'center', padding: '2rem' }}>
      {status === 'loading' && (
        <>
          <h2>Completing sign in...</h2>
          <p>Please wait while we verify your authentication.</p>
        </>
      )}
      
      {status === 'success' && (
        <>
          <h2>✓ Sign in successful!</h2>
          <p>Redirecting to dashboard...</p>
        </>
      )}
      
      {status === 'error' && (
        <>
          <h2>✗ Sign in failed</h2>
          <p>There was an error completing your sign in.</p>
          <a href="/login">Return to login</a>
        </>
      )}
    </div>
  );
};

// Registration component
export const RegisterForm: React.FC<{ apiBaseUrl: string }> = ({ apiBaseUrl }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [name, setName] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleRegister = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch(`${apiBaseUrl}/api/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ email, password, name }),
      });

      if (response.ok) {
        setSuccess(true);
        // Optionally redirect to login or show success message
      } else {
        const errorData = await response.json();
        setError(errorData.title || 'Registration failed. Please try again.');
      }
    } catch (err) {
      setError('Network error. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  if (success) {
    return (
      <div className="success-message">
        <h2>Registration Successful!</h2>
        <p>Please check your email to verify your account.</p>
        <a href="/login">Go to Login</a>
      </div>
    );
  }

  return (
    <div className="register-container">
      <h2>Create Account</h2>
      
      {error && (
        <div className="error-message" style={{ color: 'red', marginBottom: '1rem' }}>
          {error}
        </div>
      )}

      <form onSubmit={handleRegister}>
        <div className="form-group">
          <label htmlFor="name">Full Name</label>
          <input
            type="text"
            id="name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            disabled={loading}
          />
        </div>

        <div className="form-group">
          <label htmlFor="email">Email</label>
          <input
            type="email"
            id="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            disabled={loading}
          />
        </div>

        <div className="form-group">
          <label htmlFor="password">Password</label>
          <input
            type="password"
            id="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            disabled={loading}
            minLength={6}
          />
        </div>

        <button type="submit" disabled={loading}>
          {loading ? 'Creating account...' : 'Register'}
        </button>
      </form>

      <div style={{ marginTop: '1rem', textAlign: 'center' }}>
        <a href="/login">Already have an account? Sign in</a>
      </div>
    </div>
  );
};

// Logout function
export const logout = async (apiBaseUrl: string) => {
  try {
    await fetch(`${apiBaseUrl}/api/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    });
    window.location.href = '/login';
  } catch (err) {
    console.error('Logout failed:', err);
  }
};

// Hook to check authentication status
export const useAuth = (apiBaseUrl: string) => {
  const [user, setUser] = useState<{ email: string } | null>(null);
  const [loading, setLoading] = useState(true);

  React.useEffect(() => {
    const checkAuth = async () => {
      try {
        const response = await fetch(`${apiBaseUrl}/api/auth/pingauth`, {
          credentials: 'include',
        });

        if (response.ok) {
          const data = await response.json();
          setUser(data);
        } else {
          setUser(null);
        }
      } catch (err) {
        console.error('Auth check failed:', err);
        setUser(null);
      } finally {
        setLoading(false);
      }
    };

    checkAuth();
  }, [apiBaseUrl]);

  return { user, loading, isAuthenticated: !!user };
};

// Example usage in App.tsx or routing configuration:
/*
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LoginForm, AuthCallback, RegisterForm, useAuth } from './auth';

const API_BASE_URL = 'http://localhost:5000';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginForm apiBaseUrl={API_BASE_URL} />} />
        <Route path="/register" element={<RegisterForm apiBaseUrl={API_BASE_URL} />} />
        <Route path="/auth/callback" element={<AuthCallback apiBaseUrl={API_BASE_URL} />} />
        <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
      </Routes>
    </BrowserRouter>
  );
}

// Protected route component
function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth(API_BASE_URL);

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!user) {
    return <Navigate to="/login" />;
  }

  return <>{children}</>;
}
*/
