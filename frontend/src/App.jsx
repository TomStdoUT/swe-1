import { useEffect, useState } from 'react';
import { CategorizedPoll } from './components/CategorizedPoll';
import { MiniFeed } from './components/MiniFeed';
import { FullFeed } from './components/FullFeed';
import { Login } from './pages/Login';
import { PublicProfiles } from './pages/PublicProfiles';
import './App.css';

function App() {
  const [username, setUsername] = useState(null);
  const [userId, setUserId] = useState(null);
  const [userPoints, setUserPoints] = useState(0);
  const [restoring, setRestoring] = useState(() => Boolean(sessionStorage.getItem('poll-token')));
  const [currentView, setCurrentView] = useState('creator');
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [unreadNotifications, setUnreadNotifications] = useState([]);
  
  useEffect(() => {
    if (!username) return;
    
    const fetchNotifications = () => {
      fetch('/api/notifications', {
        headers: { 'Authorization': `Bearer ${sessionStorage.getItem('poll-token')}` },
      })
        .then((res) => res.json())
        .then((data) => {
          if (Array.isArray(data)) setUnreadNotifications(data);
        })
        .catch((err) => console.error(err));
    };

    fetchNotifications();
    const interval = setInterval(fetchNotifications, 10000);
    return () => clearInterval(interval);
  }, [username]);

  useEffect(() => {
    const token = sessionStorage.getItem('poll-token');
    if (!token) {
      setRestoring(false);
      return;
    }

    fetch('/api/user', {
      headers: { 'Authorization': `Bearer ${token}` },
    })
      .then((res) => res.json())
      .then((data) => {
        if (data && data.pointsBalance !== undefined) {
          setUserPoints(data.pointsBalance);
          setUsername(data.username);
          setUserId(data.id);
        }
      })
      .catch(() => {})
      .finally(() => setRestoring(false));
  }, [refreshTrigger, currentView]);

  const handleLogin = (user) => {
    sessionStorage.setItem('poll-username', user.username);
    setUsername(user.username);
    setUserId(user.id);
    setUserPoints(user.pointsBalance);
  };

  if (restoring) return null;
  if (!username) return <Login onLogin={handleLogin} />;

  return (
    <div style={{ paddingBottom: '70px' }}>
      <header className="app-user-bar">
        <span>Signed in as <strong>{username}</strong></span>
        <button type="button" onClick={() => setCurrentView('notifications')}>
          Notifications ({unreadNotifications.length})
        </button>
        <button type="button" onClick={() => setCurrentView('profiles')}>
          Public profiles
        </button>
        <button
          type="button"
          onClick={() => {
            sessionStorage.removeItem('poll-username');
            sessionStorage.removeItem('poll-token');
            setUsername(null);
            setUserPoints(100);
            setRefreshTrigger(prev => prev + 1);
          }}
        >
          Log out
        </button>
      </header>

      {currentView === 'notifications' ? (
        <div style={{ padding: '40px', maxWidth: '600px', margin: '0 auto', textAlign: 'left' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <h2>Notifications</h2>
            <button onClick={() => setCurrentView('creator')} style={{ padding: '8px 16px', borderRadius: '20px', cursor: 'pointer' }}>
              Back
            </button>
          </div>
          {unreadNotifications.length === 0 ? (
            <p style={{ color: 'var(--text)', fontStyle: 'italic' }}>No notifications yet.</p>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {unreadNotifications.map((notif) => (
                <div key={notif.id} style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '16px', background: 'var(--bg)' }}>
                  <p style={{ margin: '0 0 8px 0', fontSize: '15px' }}>{notif.content}</p>
                  <span style={{ fontSize: '12px', color: 'var(--text)' }}>{new Date(notif.createdAt).toLocaleString()}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      ) : currentView === 'profiles' ? (
        <PublicProfiles onBack={() => setCurrentView('creator')} />
      ) : currentView === 'creator' ? (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '40px', padding: '20px', alignItems: 'flex-start' }}>
          <CategorizedPoll
            userPoints={userPoints}
            onPollCreated={() => setRefreshTrigger((previous) => previous + 1)}
          />
          <MiniFeed
            onNavigate={() => setCurrentView('feed')}
            refreshTrigger={refreshTrigger}
            userId={userId}
          />
        </div>
      ) : (
        <FullFeed
          onNavigate={() => setCurrentView('creator')}
          onVoteCast={() => setRefreshTrigger((prev) => prev + 1)}
          userId={userId}
        />
      )}

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
        Points: <span style={{ color: '#4caf50', fontSize: '18px' }}>{userPoints}</span>
      </div>
    </div>
  );
}

export default App;