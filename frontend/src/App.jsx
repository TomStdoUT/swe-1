import { useEffect, useState } from 'react';
import { CategorizedPoll } from './CategorizedPoll';
import { MiniFeed, FullFeed } from './PollFeed';
import { Login } from './Login';
import './App.css';

function App() {
  const [username, setUsername] = useState(null);
  const [userPoints, setUserPoints] = useState(0); // NEW: Global points state
  const [restoring, setRestoring] = useState(() => Boolean(sessionStorage.getItem('poll-username')));
  const [currentView, setCurrentView] = useState('creator');

  // Fetch user data on load
  useEffect(() => {
    const savedUsername = sessionStorage.getItem('poll-username');
    if (!savedUsername) {
      setRestoring(false);
      return;
    }

    fetch('/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: savedUsername }),
    })
      .then((response) => {
        if (!response.ok) throw new Error('Could not restore username.');
        return response.json();
      })
      .then((data) => {
        setUsername(data.username);
        setUserPoints(data.pointsBalance); // Set points from backend
      })
      .catch(() => sessionStorage.removeItem('poll-username'))
      .finally(() => setRestoring(false));
  }, []);

  // Update this to accept points from the Login screen
  const handleLogin = (name, points) => {
    sessionStorage.setItem('poll-username', name);
    setUsername(name);
    setUserPoints(points); 
  };

  if (restoring) return null;
  if (!username) return <Login onLogin={handleLogin} />;

  return (
    <div style={{ paddingBottom: '70px' }}> {/* Adds padding so content doesn't hide behind footer */}
      <header className="app-user-bar">
        <span>Signed in as <strong>{username}</strong></span>
        <button
          type="button"
          onClick={() => {
            sessionStorage.removeItem('poll-username');
            setUsername(null);
          }}
        >
          Log out
        </button>
      </header>

      {currentView === 'creator' ? (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '40px', padding: '20px', alignItems: 'flex-start' }}>
          <CategorizedPoll />
          <MiniFeed onNavigate={() => setCurrentView('feed')} />
        </div>
      ) : (
        // Pass a callback to update points immediately after voting
        <FullFeed 
          onNavigate={() => setCurrentView('creator')} 
          onVoteCast={(newPoints) => setUserPoints(newPoints)} 
        />
      )}

      {/* NEW: Global Fixed Footer */}
      <div style={{ 
        position: 'fixed', 
        bottom: 0, 
        left: 0, 
        width: '100%', 
        background: '#fff', 
        borderTop: '1px solid var(--border)', 
        padding: '15px 20px', 
        textAlign: 'center', 
        fontWeight: 'bold',
        boxShadow: '0 -4px 12px rgba(0,0,0,0.05)',
        zIndex: 1000
      }}>
        Taškai: <span style={{ color: '#4caf50', fontSize: '18px' }}>{userPoints}</span>
      </div>
    </div>
  );
}

export default App;