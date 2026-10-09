import { useState, useEffect } from 'react';
import { TimerAndProgress } from './TimerAndProgress';

export function MiniFeed({ onNavigate, refreshTrigger, userId }) {
  const [polls, setPolls] = useState([]);

  useEffect(() => {
    fetch('/api/polls', {
      headers: { 'Authorization': `Bearer ${sessionStorage.getItem('poll-token')}` }
    })
      .then(res => res.json())
      .then(data => setPolls(data));
  }, [refreshTrigger]);

  const handleDelete = async (pollId) => {
    if (window.confirm('Are you sure you want to delete this poll?')) {
      try {
        const response = await fetch(`/api/polls/${pollId}`, {
          method: 'DELETE',
          headers: { 'Authorization': `Bearer ${sessionStorage.getItem('poll-token')}` },
        });
        
        if (response.ok) {
          setPolls(polls.filter(p => p.id !== pollId));
        } else {
          const data = await response.json();
          alert(data.error);
        }
      } catch {
        alert('Error deleting poll.');
      }
    }
  };

  return (
    <div style={{ width: '100%', maxWidth: '400px', display: 'flex', flexDirection: 'column' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
        <h2>Poll side feed</h2>
        <button onClick={onNavigate} style={{ padding: '8px 16px', borderRadius: '20px', cursor: 'pointer' }}>
          Go to Feed
        </button>
      </div>
      <div style={{ flex: 1, overflowY: 'auto', maxHeight: '75vh', paddingRight: '10px' }}>
        {polls.map(poll => (
          <div key={poll.id} style={{ border: '1px solid var(--border)', borderRadius: '8px', padding: '16px', marginBottom: '16px', position: 'relative' }}>
            {poll.creatorId === userId && (
              <button
                onClick={() => handleDelete(poll.id)}
                style={{ position: 'absolute', top: '10px', right: '10px', background: '#dc2626', color: '#fff', border: 'none', borderRadius: '4px', padding: '4px 8px', cursor: 'pointer', fontSize: '12px' }}
              >
                Delete
              </button>
            )}
            <h3 style={{ margin: '0 0 10px 0', fontSize: '18px', paddingRight: '50px' }}>{poll.question}</h3>
            <TimerAndProgress createdAt={poll.createdAt} closingTime={poll.closingTime} />
          </div>
        ))}
      </div>
    </div>
  );
}