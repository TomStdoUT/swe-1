import { useEffect, useState } from 'react';
import './PublicProfiles.css';

export function PublicProfiles({ onBack, currentUsername }) {
  const [profiles, setProfiles] = useState([]);
  const [following, setFollowing] = useState(new Set());
  const [error, setError] = useState('');
  const [updatingUser, setUpdatingUser] = useState('');

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

        const token = sessionStorage.getItem('poll-token');
        if (token) {
          const followingResponse = await fetch('/api/users/following', {
            headers: { Authorization: `Bearer ${token}` },
          });
          if (followingResponse.ok) {
            const followedProfiles = await followingResponse.json();
            if (isActive) setFollowing(new Set(followedProfiles.map((profile) => profile.id)));
          }
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

  const toggleFollow = async (profile) => {
    const shouldFollow = !following.has(profile.id);
    setUpdatingUser(profile.username);

    try {
      const response = await fetch(`/api/users/${encodeURIComponent(profile.username)}/follow`, {
        method: shouldFollow ? 'POST' : 'DELETE',
        headers: { Authorization: `Bearer ${sessionStorage.getItem('poll-token')}` },
      });

      if (!response.ok) {
        throw new Error('Could not update follow status. Please try again.');
      }

      setFollowing((previous) => {
        const updated = new Set(previous);
        if (shouldFollow) updated.add(profile.id);
        else updated.delete(profile.id);
        return updated;
      });
      setProfiles((previous) => previous.map((item) => item.id === profile.id
        ? { ...item, followerCount: item.followerCount + (shouldFollow ? 1 : -1) }
        : item));
    } catch (followError) {
      setError(followError.message);
    } finally {
      setUpdatingUser('');
    }
  };

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
              <div>
                <dt>Followers</dt>
                <dd>{profile.followerCount}</dd>
              </div>
              <div>
                <dt>Following</dt>
                <dd>{profile.followingCount}</dd>
              </div>
            </dl>
            {currentUsername?.toLowerCase() !== profile.username.toLowerCase() && (
              <button
                className="profile-follow-button"
                type="button"
                disabled={updatingUser === profile.username}
                onClick={() => toggleFollow(profile)}
              >
                {updatingUser === profile.username
                  ? 'Saving…'
                  : following.has(profile.id) ? 'Unfollow' : 'Follow'}
              </button>
            )}
          </article>
        ))}
      </div>
    </main>
  );
}