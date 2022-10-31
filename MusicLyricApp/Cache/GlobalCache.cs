﻿using System.Collections.Generic;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Cache
{
    public static class GlobalCache
    {
        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, IEnumerable<string>> PlaylistSongIdsCache =
            new Dictionary<string, IEnumerable<string>>();

        public static bool ContainsPlaylistSongIds(string playlistId)
        {
            return PlaylistSongIdsCache.ContainsKey(playlistId);
        }

        public static IEnumerable<string> GetSongIdsFromPlaylist(string playlistId)
        {
            PlaylistSongIdsCache.TryGetValue(playlistId, out var result);
            return result;
        }

        public static void PutPlaylistSongIds(string playlistId, IEnumerable<string> songIds)
        {
            PlaylistSongIdsCache.Add(playlistId, songIds);
        }
        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, IEnumerable<string>> AlbumSongIdsCache =
            new Dictionary<string, IEnumerable<string>>();

        public static bool ContainsAlbumSongIds(string albumId)
        {
            return AlbumSongIdsCache.ContainsKey(albumId);
        }

        public static IEnumerable<string> GetSongIdsFromAlbum(string albumId)
        {
            AlbumSongIdsCache.TryGetValue(albumId, out var result);
            return result;
        }

        public static void PutAlbumSongIds(string albumId, IEnumerable<string> songIds)
        {
            AlbumSongIdsCache.Add(albumId, songIds);
        }

        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, SongVo> SongCache = new Dictionary<string, SongVo>();

        public static bool ContainsSong(string songId)
        {
            return SongCache.ContainsKey(songId);
        }

        public static SongVo GetSong(string songId)
        {
            SongCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutSong(string songId, SongVo song)
        {
            SongCache.Add(songId, song);
        }

        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, SaveVo> SaveVoCache = new Dictionary<string, SaveVo>();

        public static bool ContainsSaveVo(string songId)
        {
            return SaveVoCache.ContainsKey(songId);
        }

        public static SaveVo GetSaveVo(string songId)
        {
            SaveVoCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutSaveVo(string songId, SaveVo saveVo)
        {
            SaveVoCache.Add(songId, saveVo);
        }

        /* --------------------------------------------------------------------------------------------------------- */

        private static readonly Dictionary<string, LyricVo> LyricCache = new Dictionary<string, LyricVo>();

        public static bool ContainsLyric(string songId)
        {
            return LyricCache.ContainsKey(songId);
        }

        public static LyricVo GetLyric(string songId)
        {
            LyricCache.TryGetValue(songId, out var result);
            return result;
        }

        public static void PutLyric(string songId, LyricVo albumResult)
        {
            LyricCache.Add(songId, albumResult);
        }
    }
}