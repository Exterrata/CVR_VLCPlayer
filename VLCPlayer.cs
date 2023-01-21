using UnityEngine;
using System;
using System.Collections.Generic;
using ABI_RC.VideoPlayer.Scripts.Players;
using ABI_RC.VideoPlayer.Scripts;
using ABI_RC.VideoPlayer;
using ABI.CCK.Components;
using LibVLCSharp;
using MelonLoader;

//built off of the unity-vlc script "VLCPlayerExample"
namespace VLCTest
{
	public class VLCPlayer : IVideoPlayerPlayer, IVideoPlayerInfo, IVideoSubtitles, IVideoMetaData
	{
		public static LibVLC libVLC; //The LibVLC class is mainly used for making MediaPlayer and Media objects. You should only have one LibVLC instance.
		private MediaPlayer _mediaPlayer; //MediaPlayer is the main class we use to interact with VLC

		private Texture2D _vlcTexture = null; //This is the texture libVLC writes to directly. It's private.

		private string _url = ""; //Can be a web path or a local path

		private bool _playing;
		private bool _paused;

		public event DPlayerEvent PlayerEvent;

		public VLCPlayer(CVRVideoPlayer cvrVideoPlayer)
		{

		}

		#region controls

		public void Enable()
        {
			//Setup LibVLC
			if (libVLC == null)
				CreateLibVLC();

			//Setup Media Player
			CreateMediaPlayer();

			//start task of updating texture
			_playing = true;
        }

		public void Disable()
        {
			_playing = false;
		}

		public void Destroy() => DestroyMediaPlayer();

		public void SetUrl(string url, bool isPaused)
        {
			_url = url;
			if (_mediaPlayer.Media != null)
				_mediaPlayer.Media.Dispose();
			var trimmedPath = _url.Trim(new char[] { '"' });//Windows likes to copy paths with quotes but Uri does not like to open them
			_mediaPlayer.Media = new Media(new Uri(trimmedPath));
			if (!isPaused)
				Play();
		}

		public bool Play()
		{
			_mediaPlayer.Play();
			_playing = true;
			_paused = false;
			return true;
		}

		public void Pause()
		{
			_mediaPlayer.Pause();
			_playing = false;
			_paused = true;
		}

		public void Seek(long timeDelta)
		{
			_mediaPlayer.SetTime(_mediaPlayer.Time + timeDelta);
		}

		public YoutubeDl.ProcessResult? ProcessResult => new YoutubeDl.ProcessResult();

		public VideoPlayerUtils.AudioChannelMask GetAudioChannels() => VideoPlayerUtils.AudioChannelMask.Unspecified;

		public bool IsLivestream => false;

		public double GetDuration() => _mediaPlayer.Media.Duration * 0.001;

		public float GetPlayerFps() => _mediaPlayer.Rate;

		public float GetVideoFps() => _mediaPlayer.Rate;

		public string GetTitle() => _mediaPlayer.Title.ToString();

		public string GetUrl() => _url;

		public int GetVideoWidth()
		{
			uint height = 0;
			uint width = 0;
			_mediaPlayer.Size(0, ref width, ref height);
			return (int)width;
		}

		public int GetVideoHeight()
		{
			uint height = 0;
			uint width = 0;
			_mediaPlayer.Size(0, ref width, ref height);
			return (int)height;
		}
		public void SetPlaybackSpeed(float speed)
        {
			//idk
        }

		public void SetAudioPlaybackMode(VideoPlayerUtils.AudioMode audioMode)
        {
			//idk
        }

		public VideoPlayerUtils.PlayerState GetState()
        {
			if (_playing)
				return VideoPlayerUtils.PlayerState.Playing;
			if (_paused)
				return VideoPlayerUtils.PlayerState.Paused;
			return VideoPlayerUtils.PlayerState.NotPlaying;	
        }

		public double GetMaxBufferedTime() => 0;

		public bool IsMediaOpen() => _playing;

		public bool IsPlaying() => _playing;

		public bool IsConnectionLost() => false;

		public double AudioTime => _mediaPlayer.Time;

		public float Volume
		{
			get
			{
				if (_mediaPlayer == null)
					return 0;
				return _mediaPlayer.Volume;
			}
			set => _mediaPlayer.SetVolume((int)value);
		}

		public long Duration
		{
			get
			{
				if (_mediaPlayer == null || _mediaPlayer.Media == null)
					return 0;
				return _mediaPlayer.Media.Duration;
			}
		}

		public double Time
		{
			get
			{
				if (_mediaPlayer == null)
					return 0;
				return _mediaPlayer.Time;
			}
			set => _mediaPlayer.SetTime((long)value * 1000);
		}

		public string GetSubtitle()
        {
			return "";
        }

		public string[] GetAvailableSubtitles()
        {
			return new string[0];
        }

		public string SelectedSubtitle { get; set; }

		public IVideoPlayerInfo Info => (IVideoPlayerInfo)this;

		public IVideoMetaData VideoMetaData => (IVideoMetaData)this;

		public IVideoSubtitles Subtitles => (IVideoSubtitles)this;

		public List<MediaTrack> Tracks(TrackType type)
		{
			return ConvertMediaTrackList(_mediaPlayer?.Tracks(type));
		}

		public MediaTrack SelectedTrack(TrackType type)
		{
			return _mediaPlayer?.SelectedTrack(type);
		}

		public void Select(MediaTrack track)
		{
			_mediaPlayer?.Select(track);
		}

		public void Unselect(TrackType type)
		{
			_mediaPlayer?.Unselect(type);
		}

		public Texture GetVideoTexture()
        {
			return _vlcTexture;
        }

		//This returns the video orientation for the currently playing video, if there is one
		public VideoOrientation? GetVideoOrientation()
		{
			var tracks = _mediaPlayer?.Tracks(TrackType.Video);

			if (tracks == null || tracks.Count == 0)
				return null;

			var orientation = tracks[0]?.Data.Video.Orientation; //At the moment we're assuming the track we're playing is the first track

			return orientation;
		}

		#endregion

		#region internal

		//Create a new static LibVLC instance and dispose of the old one. You should only ever have one LibVLC instance.
		void CreateLibVLC()
		{
			//Dispose of the old libVLC if necessary
			if (libVLC != null)
			{
				libVLC.Dispose();
				libVLC = null;
			}
			Core.Initialize(Application.dataPath); //Load VLC dlls
			libVLC = new LibVLC(enableDebugLogs: true); //You can customize LibVLC with advanced CLI options here https://wiki.videolan.org/VLC_command-line_help/

			//Setup Error Logging
			Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
			libVLC.Log += (s, e) =>
			{
			//Always use try/catch in LibVLC events.
			//LibVLC can freeze Unity if an exception goes unhandled inside an event handler.
			try
				{
					Debug.Log(e.FormattedLog);

				}
				catch (Exception ex)
				{
					Debug.Log("Exception caught in libVLC.Log: \n" + ex.ToString());
				}

			};
		}

		//Create a new MediaPlayer object and dispose of the old one. 
		void CreateMediaPlayer()
		{
			if (_mediaPlayer != null)
			{
				DestroyMediaPlayer();
			}
			_mediaPlayer = new MediaPlayer(libVLC);
		}

		//Dispose of the MediaPlayer object. 
		void DestroyMediaPlayer()
		{
			_mediaPlayer?.Stop();
			_mediaPlayer?.Dispose();
			_mediaPlayer = null;
		}

		//Resize the output textures to the size of the video
		void ResizeOutputTextures(uint px, uint py)
		{
			var texptr = _mediaPlayer.GetTexture(px, py, out bool updated);
			if (px != 0 && py != 0 && updated && texptr != IntPtr.Zero)
			{
				//If the currently playing video uses the Bottom Right orientation, we have to do this to avoid stretching it.
				if (GetVideoOrientation() == VideoOrientation.BottomRight)
				{
					uint swap = px;
					px = py;
					py = swap;
				}

				_vlcTexture = Texture2D.CreateExternalTexture((int)px, (int)py, TextureFormat.RGBA32, false, true, texptr); //Make a texture of the proper size for the video to output to
			}
		}

		//Converts MediaTrackList objects to Unity-friendly generic lists. Might not be worth the trouble.
		List<MediaTrack> ConvertMediaTrackList(MediaTrackList tracklist)
		{
			if (tracklist == null)
				return new List<MediaTrack>(); //Return an empty list

			var tracks = new List<MediaTrack>((int)tracklist.Count);
			for (uint i = 0; i < tracklist.Count; i++)
			{
				tracks.Add(tracklist[i]);
			}
			return tracks;
		}

		#endregion
	}
}