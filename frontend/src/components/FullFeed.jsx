import { useCallback, useEffect, useState } from 'react';
import { FullPollCard } from './FullPollCard';

const CATEGORIES = ['All', 'Sport', 'Technology', 'Politics', 'Fun', 'Other'];

export function FullFeed({ onNavigate, onVoteCast, userId }) {
  const [polls, setPolls] = useState([]);
  const [selectedCategory, setSelectedCategory] = useState('All');
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const fetchPolls = useCallback(async () => {
    try {
      const response = await fetch('/api/polls', {
        headers: {
          Authorization: `Bearer ${sessionStorage.getItem('poll-token')}`,
        },
      });

      if (!response.ok) {
        throw new Error('Failed to load polls.');
      }

      const data = await response.json();
      setPolls(Array.isArray(data) ? data : []);
      setError('');
    } catch {
      setError('Could not load polls. Please try again.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPolls();
  }, [fetchPolls]);

  const handleVote = async (pollId, optionIndices, bettedPoints) => {
    try {
      const response = await fetch(`/api/polls/${pollId}/vote`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${sessionStorage.getItem('poll-token')}`,
        },
        body: JSON.stringify({ optionIndices, bettedPoints }),
      });

      const data = await response.json().catch(() => ({}));
      if (!response.ok) {
        alert(data.error || 'Failed to cast vote.');
        return false;
      }

      await fetchPolls();
      if (onVoteCast) onVoteCast();
      return true;
    } catch {
      alert('Error submitting vote.');
      return false;
    }
  };

  const handleDelete = async (pollId) => {
    if (!window.confirm('Are you sure you want to delete this poll?')) return;

    try {
      const response = await fetch(`/api/polls/${pollId}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${sessionStorage.getItem('poll-token')}`,
        },
      });

      if (response.ok) {
        setPolls((prev) => prev.filter((p) => p.id !== pollId));
      } else {
        const data = await response.json().catch(() => ({}));
        alert(data.error || 'Failed to delete poll.');
      }
    } catch {
      alert('Error deleting poll.');
    }
  };

  const handleBoosted = () => {
    fetchPolls();
    if (onVoteCast) onVoteCast();
  };

  const normalizedQuery = searchQuery.trim().toLowerCase();
  const filteredPolls = polls.filter((poll) => {
    const matchesCategory = selectedCategory === 'All' || poll.category === selectedCategory;
    if (!matchesCategory) return false;

    if (!normalizedQuery) return true;
    const inQuestion = poll.question?.toLowerCase().includes(normalizedQuery);
    const inTags = Array.isArray(poll.tags)
      && poll.tags.some((tag) => tag.toLowerCase().includes(normalizedQuery));
    return inQuestion || inTags;
  });

  return (
    <div style={{ maxWidth: '720px', margin: '0 auto', padding: '24px 20px' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <h2 style={{ margin: 0 }}>Poll Feed</h2>
        <button
          type="button"
          onClick={onNavigate}
          style={{ padding: '8px 16px', borderRadius: '20px', cursor: 'pointer' }}
        >
          Back to Creator
        </button>
      </div>

      <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', marginBottom: '14px' }}>
        {CATEGORIES.map((category) => {
          const isActive = selectedCategory === category;
          return (
            <button
              key={category}
              type="button"
              onClick={() => setSelectedCategory(category)}
              style={{
                padding: '6px 14px',
                borderRadius: '16px',
                border: '1px solid var(--border)',
                background: isActive ? 'var(--accent)' : 'var(--bg)',
                color: isActive ? '#fff' : 'var(--text-h)',
                cursor: 'pointer',
                fontWeight: isActive ? 'bold' : 'normal',
              }}
            >
              {category}
            </button>
          );
        })}
      </div>

      <div style={{ marginBottom: '20px' }}>
        <input
          type="text"
          placeholder="Search polls by question or tag..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          style={{
            width: '100%',
            padding: '10px 12px',
            boxSizing: 'border-box',
            borderRadius: '6px',
            border: '1px solid var(--border)',
          }}
        />
      </div>

      {error && (
        <p style={{ color: '#dc2626', marginBottom: '16px' }} role="alert">
          {error}
        </p>
      )}

      {isLoading ? (
        <p style={{ color: 'var(--text)', fontStyle: 'italic' }}>Loading polls...</p>
      ) : filteredPolls.length === 0 ? (
        <p style={{ color: 'var(--text)', fontStyle: 'italic' }}>No polls match your filter.</p>
      ) : (
        filteredPolls.map((poll) => (
          <FullPollCard
            key={poll.id}
            poll={poll}
            onVote={handleVote}
            onDelete={handleDelete}
            onBoosted={handleBoosted}
            userId={userId}
            onPollFinished={fetchPolls}
          />
        ))
      )}
    </div>
  );
}