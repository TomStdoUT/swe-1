import { useState, useEffect } from 'react';

const CATEGORIES = ['Sport', 'Technology', 'Politics', 'Fun', 'Other'];

export function CategorizedPoll({ onPollCreated }) {
  const [question, setQuestion] = useState('');
  const [category, setCategory] = useState(CATEGORIES[0]);
  const [tagInput, setTagInput] = useState('');
  const [tags, setTags] = useState([]);
  const [options, setOptions] = useState(['', '']); 
  const [userPoints, setUserPoints] = useState(100);
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  // NEW: State for timer and multiple choice
  const [durationHours, setDurationHours] = useState(24);
  const [allowMultiple, setAllowMultiple] = useState(false);

  useEffect(() => {
    fetch('/api/user')
      .then((res) => res.json())
      .then((data) => {
        if (data.pointsBalance !== undefined) {
          setUserPoints(data.pointsBalance);
        }
      })
      .catch((err) => console.error('Error fetching user points:', err));
  }, []);

  const handleAddTag = () => {
    if (tagInput.trim() && !tags.includes(tagInput.trim())) {
      setTags([...tags, tagInput.trim()]);
      setTagInput('');
    }
  };

  const handleRemoveTag = (tagToRemove) => {
    setTags(tags.filter((tag) => tag !== tagToRemove));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const finalTags = [...tags];
    if (tagInput.trim() && !tags.includes(tagInput.trim())) {
      finalTags.push(tagInput.trim());
      setTagInput(''); 
    }

    // NEW: Updated payload without pointsBet, added duration and multiple choice
    const pollData = {
      question,
      category,
      tags: finalTags,
      options,
      durationHours, 
      allowMultiple
    };
    
    setIsSubmitting(true);
    try {
      const response = await fetch('/api/polls', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(pollData),
      });

      if (!response.ok) {
        const errorData = await response.json();
        alert(`Error: ${errorData.error}`);
        return;
      }

      const result = await response.json();
      setUserPoints(result.remainingPoints);
      
      alert('Success creating poll!');

      if (onPollCreated) onPollCreated();
      
      // Reset form
      setQuestion('');
      setOptions(['', '']);
      setTags([]);
      setDurationHours(24);
      setAllowMultiple(false);
    } catch (error) {
      console.error('Failed to connect to backend:', error);
      alert('Could not connect to backend server.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ padding: '20px', width: '100%', maxWidth: '450px', margin: '0 auto' }}>
      
      <h2 style={{ textAlign: 'center', marginBottom: '25px' }}>Create a poll with a category</h2>

      <div style={{ marginBottom: '15px' }}>
        <label style={{ display: 'block', textAlign: 'center', marginBottom: '5px' }}>Question:</label>
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          required
          style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
        />
      </div>

      <div style={{ marginBottom: '15px' }}>
        <label style={{ display: 'block', textAlign: 'center', marginBottom: '5px' }}>Choices (2 - 6):</label>
        {options.map((opt, index) => (
          <div key={index} style={{ display: 'flex', gap: '5px', marginTop: '5px' }}>
            <input
              type="text"
              value={opt}
              onChange={(e) => {
                const newOpts = [...options];
                newOpts[index] = e.target.value;
                setOptions(newOpts);
              }}
              required
              placeholder={`Choice ${index + 1}`}
              style={{ flex: 1, padding: '8px' }}
            />
            {options.length > 2 && (
              <button type="button" onClick={() => setOptions(options.filter((_, i) => i !== index))}>X</button>
            )}
          </div>
        ))}
        
        {/* NEW: Options toggle and Add button aligned properly */}
        <div style={{ display: 'flex', justifyContent: 'center', gap: '10px', marginTop: '10px' }}>
          {options.length < 6 && (
            <button type="button" onClick={() => setOptions([...options, ''])} style={{ padding: '6px 12px' }}>
              + Add new choice
            </button>
          )}
          <button 
            type="button" 
            onClick={() => setAllowMultiple(!allowMultiple)}
            style={{ padding: '6px 12px', background: allowMultiple ? 'var(--accent)' : 'var(--bg)', color: allowMultiple ? '#fff' : 'var(--text-h)', border: '1px solid var(--border)' }}
          >
            {allowMultiple ? 'Multiple choices' : 'One answer'}
          </button>
        </div>
      </div>

      <div style={{ marginBottom: '15px' }}>
        <label style={{ display: 'block', textAlign: 'center', marginBottom: '5px' }}>Category:</label>
        <select
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
        >
          {CATEGORIES.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: '15px' }}>
        <label style={{ display: 'block', textAlign: 'center', marginBottom: '5px' }}>Tags:</label>
        <div style={{ display: 'flex', gap: '5px' }}>
          <input
            type="text"
            value={tagInput}
            onChange={(e) => setTagInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault();
                handleAddTag();
              }
            }}
            placeholder="Enter a tag"
            style={{ flex: 1, padding: '8px' }}
          />
          <button type="button" onClick={handleAddTag}>Pridėti</button>
        </div>
        <div style={{ marginTop: '8px', display: 'flex', gap: '5px', flexWrap: 'wrap', justifyContent: 'center' }}>
          {tags.map((tag) => (
            <span
              key={tag}
              style={{ background: '#e0e0e0', padding: '4px 8px', borderRadius: '4px', cursor: 'pointer', color: '#000' }}
              onClick={() => handleRemoveTag(tag)}
            >
              #{tag} &times;
            </span>
          ))}
        </div>
      </div>

      {/* NEW: Timer input replaces Points to Stake */}
      <div style={{ marginBottom: '20px' }}>
        <label style={{ display: 'block', textAlign: 'center', marginBottom: '5px' }}>Poll duration (hours):</label>
        <input
          type="number"
          min="1"
          max="168"
          value={durationHours}
          onChange={(e) => setDurationHours(Math.max(1, parseInt(e.target.value) || 1))}
          style={{ width: '100%', padding: '8px', boxSizing: 'border-box' }}
        />
      </div>

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: '20px' }}>
        <div style={{ 
          background: '#fff', 
          color: '#000', 
          padding: '8px 16px', 
          border: '1px solid var(--border)', 
          borderRadius: '6px', 
          fontSize: '14px', 
          fontWeight: 'bold',
          boxShadow: 'var(--shadow)'
        }}>
          Points: <span style={{ color: '#4caf50' }}>{userPoints}</span>
        </div>
        
        <button type="submit" disabled={isSubmitting} style={{ padding: '10px 20px', fontWeight: 'bold' }}>
          {isSubmitting ? 'Saving...' : 'Save the poll to feed'}
        </button>
      </div>
    </form>
  );
}