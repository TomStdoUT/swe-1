import { useState } from 'react';

const CATEGORIES = ['Sportas', 'Technologijos', 'Politika', 'Pramogos', 'Kita'];

export function CategorizedPoll() {
  const [question, setQuestion] = useState('');
  const [category, setCategory] = useState(CATEGORIES[0]);
  const [tagInput, setTagInput] = useState('');
  const [tags, setTags] = useState([]);

  const handleAddTag = () => {
    if (tagInput.trim() && !tags.includes(tagInput.trim())) {
      setTags([...tags, tagInput.trim()]);
      setTagInput('');
    }
  };

  const handleRemoveTag = (tagToRemove) => {
    setTags(tags.filter((tag) => tag !== tagToRemove));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const pollData = {
      question,
      category,
      tags,
      createdAt: new Date().toISOString(),
    };
    console.log('Sukurta apklausa su kategorija:', pollData);
    alert(`Apklausa sukurta! Kategorija: ${category}, Žymos: ${tags.join(', ')}`);
  };

  return (
    <form onSubmit={handleSubmit} style={{ padding: '20px', maxWidth: '400px' }}>
      <h2>Sukurti apklausą su kategorija (Issue #4)</h2>

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

      <button type="submit" style={{ padding: '10px 15px', marginTop: '10px' }}>
        Išsaugoti apklausą
      </button>
    </form>
  );
}