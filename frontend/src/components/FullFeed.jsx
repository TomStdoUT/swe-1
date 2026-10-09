import { useState, useEffect } from 'react';
import { FullPollCard } from './FullPollCard';

export function FullFeed({ onNavigate, onVoteCast, userId }) {
  const [polls, setPolls] = useState([]);

  const loadPolls = () => {
    fetch('/api/polls', {
      headers: { 'Authorization': `Bearer ${sessionStorage.getItem('poll-token')}` }
    })
      .then(res => res.json())
      .then(data => setPolls(data));
  };

  useEffect(() => {
    loadPolls();
  }, []);

  const handlePollFinished = () => {
    setTimeout(async () => {
      loadPolls();
      const token = sessionStorage.getItem('poll-token');
      if (token) {
        try {
          const userRes = await fetch('/api/user', { headers: { 'Authorization': `Bearer ${token}` } });
          if (userRes.ok) {
            onVoteCast();
          }
        } catch (e) {
          console.error(e);
        }
      }
    }, 1000);
  };

  const handleVote = async (pollId, optionIndices, bettedPoints) => {
    try {
      const response = await fetch(`/api/polls/${pollId}/vote`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${sessionStorage.getItem('poll-token')}`,
        },
        body: JSON.stringify({ optionIndices, bettedPoints }) 
      });
      
      const data = await response.json();
      
      if (!response.ok) {
        const errorMessage = data.error || (data.errors && Object.values(data.errors).join(', ')) || 'Unknown error';
        throw new Error(errorMessage);
      }
      
      onVoteCast(); 
      loadPolls(); 
      return true;
    } catch (error) {
      alert(`Error: ${error.message}`);
      return false;
    }
  };

  const handleDelete = async (pollId) => {
    if (window.confirm('Are you sure you want to delete this poll? All your user choices will be lost.')) {
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
    <div style={{ padding: '40px', maxWidth: '800px', margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '30px' }}>
        <h1>Poll feed</h1>
        <button onClick={onNavigate} style={{ padding: '8px 16px', borderRadius: '20px', cursor: 'pointer' }}>
          ← Back to creation
        </button>
      </div>

      {polls.map(poll => (
        <FullPollCard
          key={poll.id}
          poll={poll}
          onVote={handleVote}
          onDelete={handleDelete}
          userId={userId}
          onPollFinished={handlePollFinished}
        />
      ))}
    </div>
  );
}