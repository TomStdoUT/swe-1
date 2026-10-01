import { useEffect, useState } from 'react';
import './PublicProfiles.css';

export function PublicProfiles({ onBack }) {
  const [profiles, setProfiles] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    let isActive = true;

    const loadProfiles = async () => {
      try {
        const response = await fetch('/api/users');
        if (!response.ok) {
          throw new Error('Could not load profiles.');
        }

        const data = await response.json();
        if (isActive) {
          setProfiles(data);
          setError('');
        }
      } catch {
        if (isActive) {
          setError('Could not load profiles. Please try again.');
        }
      }
    };

    loadProfiles();
    const refreshInterval = setInterval(loadProfiles, 5000);

    return () => {
      isActive = false;
      clearInterval(refreshInterval);
    };
  }, []);

  return (
    <main className="profiles-page">
      <div className="profiles-heading">
        <div>
          <h1>Public profiles</h1>
          <p>Points and accuracy update as polls close.</p>
        </div>
        <button type="button" onClick={onBack}>
          Back
        </button>
      </div>

      {error && <p className="profiles-error" role="alert">{error}</p>}
      {!error && profiles.length === 0 && <p>No profiles yet.</p>}

      <div className="profile-list">
        {profiles.map((profile) => (
          <article className="profile-card" key={profile.id}>
            <h2>{profile.username}</h2>
            <dl>
              <div>
                <dt>Points</dt>
                <dd>{profile.pointsBalance}</dd>
              </div>
              <div>
                <dt>Accuracy</dt>
                <dd>
                  {profile.accuracyRate == null
                    ? '—'
                    : `${profile.accuracyRate}%`}
                </dd>
              </div>
              <div>
                <dt>Correct predictions</dt>
                <dd>
                  {profile.correctPredictions} / {profile.totalResolvedVotes}
                </dd>
              </div>
            </dl>
          </article>
        ))}
      </div>
    </main>
  );
}
