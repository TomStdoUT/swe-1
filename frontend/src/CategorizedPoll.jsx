import { useState, useEffect } from 'react';

const CATEGORIES = ['Sportas', 'Technologijos', 'Politika', 'Pramogos', 'Kita'];

export function CategorizedPoll() {
  const [question, setQuestion] = useState('');
  const [category, setCategory] = useState(CATEGORIES[0]);
  const [tagInput, setTagInput] = useState('');
  const [tags, setTags] = useState([]);
  const [userPoints, setUserPoints] = useState(100);
  const [pointsBet, setPointsBet] = useState(0);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Fetch initial user points from backend on load
  useEffect(() => {
    fetch('http://localhost:5000/api/user')
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

    if (pointsBet > userPoints) {
      alert('Insuficient points balance!');
      return;
    }

    const pollData = {
      question,
      category,
      tags,
      bettedPoints: pointsBet,
    };
setIsSubmitting(true);
    try {
      // Send POST request to web API
      const response = await fetch('http://localhost:5000/api/polls', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(pollData),
      });

      if (!response.ok) {
        const errorData = await response.json();
        alert(`Error: ${errorData.error}`);
        return;
      }

      const result = await response.json();

      // Update local state with the actual remaining points from backend
      setUserPoints(result.remainingPoints);
      setPointsBet(0);

      alert(`Poll created successfully! Staked: ${pointsBet}. Remaining balance: ${result.remainingPoints}`);
    } catch (error) {
      console.error('Failed to connect to backend:', error);
      alert('Could not connect to backend server.');
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ padding: '20px', maxWidth: '400px' }}>
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '15px' }}>
      <h2>Sukurti apklausą su kategorija (Issue #4)</h2>
      <div style={{ background: '#2a2a2a', color: '#fff', padding: '6px 12px', borderRadius: '6px', fontSize: '14px', fontWeight: 'bold' }}>
        Taškai: <span style={{ color: '#4caf50' }}>{userPoints}</span>
      </div>
    </div>

      <div style={{ marginBottom: '10px' }}>
        <label>Klausimas:</label>
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          required
          style={{ width: '100%', padding: '8px', marginTop: '5px' }}
        />
      </div>

      <div style={{ marginBottom: '10px' }}>
        <label>Variantai (2 - 6):</label>
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
              placeholder={`Variantas ${index + 1}`}
              style={{ flex: 1, padding: '8px' }}
            />
            {options.length > 2 && (
              <button type="button" onClick={() => setOptions(options.filter((_, i) => i !== index))}>X</button>
            )}
          </div>
        ))}
        {options.length < 6 && (
          <button type="button" onClick={() => setOptions([...options, ''])} style={{ marginTop: '5px' }}>+ Pridėti variantą</button>
        )}
      </div>

      <div style={{ marginBottom: '10px' }}>
        <label>Kategorija:</label>
        <select
          value={category}
          onChange={(e) => setCategory(e.target.value)}
          style={{ width: '100%', padding: '8px', marginTop: '5px' }}
        >
          {CATEGORIES.map((cat) => (
            <option key={cat} value={cat}>
              {cat}
            </option>
          ))}
        </select>
      </div>

      <div style={{ marginBottom: '10px' }}>
        <label>Žymos (Tags):</label>
        <div style={{ display: 'flex', gap: '5px', marginTop: '5px' }}>
          <input
            type="text"
            value={tagInput}
            onChange={(e) => setTagInput(e.target.value)}
            placeholder="Įveskite žymą"
            style={{ flex: 1, padding: '8px' }}
          />
          <button type="button" onClick={handleAddTag}>
            Pridėti
          </button>
        </div>
        <div style={{ marginTop: '8px', display: 'flex', gap: '5px', flexWrap: 'wrap' }}>
          {tags.map((tag) => (
            <span
              key={tag}
              style={{
                background: '#e0e0e0',
                padding: '4px 8px',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
              onClick={() => handleRemoveTag(tag)}
            >
              #{tag} &times;
            </span>
          ))}
        </div>
      </div>
      <div style={{ marginBottom: '10px' }}>
        <label>Statomi taškai (Points to Stake):</label>
        <input
          type="number"
          min="0"
          max={userPoints}
          value={pointsBet}
          onChange={(e) => setPointsBet(Math.max(0, parseInt(e.target.value) || 0))}
          style={{ width: '100%', padding: '8px', marginTop: '5px' }}
        />
      </div>

      <button type="submit" style={{ padding: '10px 15px', marginTop: '10px' }}>
        Išsaugoti apklausą
      </button>
    </form>
  );
}