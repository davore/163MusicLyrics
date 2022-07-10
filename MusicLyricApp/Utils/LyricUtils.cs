﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicLyricApp.Bean;

namespace MusicLyricApp.Utils
{
    /// <summary>
    /// 歌词处理基类
    /// </summary>
    public abstract class LyricUtils
    {
        /// <summary>
        /// 获取输出结果
        /// </summary>
        /// <param name="lyricVo"></param>
        /// <param name="searchInfo"></param>
        /// <returns></returns>
        public static async Task<string> GetOutputContent(LyricVo lyricVo, SearchInfo searchInfo)
        {
            var dotType = searchInfo.SettingBean.Param.DotType;
            
            var voList = await FormatLyric(lyricVo.Lyric, lyricVo.TranslateLyric, searchInfo);

            if (searchInfo.SettingBean.Param.OutputFileFormat == OutputFormatEnum.SRT)
            {
                var timestampFormat = searchInfo.SettingBean.Param.SrtTimestampFormat;
                return SrtUtils.LrcToSrt(voList, timestampFormat, dotType, lyricVo.Duration);
            }
            else
            {
                var timestampFormat = searchInfo.SettingBean.Param.LrcTimestampFormat;
                return string.Join(Environment.NewLine,  from o in voList select o.Print(timestampFormat, dotType));
            }
        }

        /// <summary>
        /// 歌词格式化
        /// </summary>
        /// <param name="originLrc">原始的歌词内容</param>
        /// <param name="translateLrc">原始的译文内容</param>
        /// <param name="searchInfo">处理参数</param>
        /// <returns></returns>
        private static async Task<List<LyricLineVo>> FormatLyric(string originLrc, string translateLrc, SearchInfo searchInfo)
        {
            var showLrcType = searchInfo.SettingBean.Param.ShowLrcType;
            var searchSource = searchInfo.SettingBean.Param.SearchSource;

            var originLyrics = SplitLrc(originLrc, searchSource);

            /*
             * 1、原文歌词不存在
             * 2、不存在翻译歌词
             * 3、选择仅原歌词
             */
            if (originLyrics.Count == 0 || string.IsNullOrEmpty(translateLrc) ||
                showLrcType == ShowLrcTypeEnum.ONLY_ORIGIN)
            {
                return originLyrics;
            }

            // 译文处理，启用罗马音进行转换，否则使用原始的译文
            var romajiConfig = searchInfo.SettingBean.Config.RomajiConfig;
            
            var translateLyrics = SplitLrc(translateLrc, searchSource);

            if (romajiConfig.Enable)
            {
                translateLyrics = await RomajiUtils.ToRomaji(originLyrics, translateLyrics, romajiConfig);
            }

            /*
             * 1、译文歌词不存在
             * 2、选择仅译文歌词
             */
            if (translateLyrics.Count == 0 || showLrcType == ShowLrcTypeEnum.ONLY_TRANSLATE)
            {
                return translateLyrics;
            }

            List<LyricLineVo> res;
            switch (showLrcType)
            {
                case ShowLrcTypeEnum.ORIGIN_PRIOR_ISOLATED:
                    res = originLyrics;
                    res.AddRange(translateLyrics);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR_ISOLATED:
                    res = translateLyrics;
                    res.AddRange(originLyrics);
                    break;
                case ShowLrcTypeEnum.ORIGIN_PRIOR_STAGGER:
                    res = SortLrc(originLyrics, translateLyrics, true);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR_STAGGER:
                    res = SortLrc(originLyrics, translateLyrics, false);
                    break;
                case ShowLrcTypeEnum.ORIGIN_PRIOR_MERGE:
                    res = MergeLrc(originLyrics, translateLyrics, searchInfo.SettingBean.Param.LrcMergeSeparator, true);
                    break;
                case ShowLrcTypeEnum.TRANSLATE_PRIOR_MERGE:
                    res = MergeLrc(originLyrics, translateLyrics, searchInfo.SettingBean.Param.LrcMergeSeparator, false);
                    break;
                default:
                    throw new NotSupportedException("not support showLrcType: " + showLrcType);
            }

            return DealTranslateLyricDefaultRule(res, searchInfo.SettingBean.Config.TranslateLyricDefaultRule);
        }

        /**
         * 切割歌词
         */
        private static List<LyricLineVo> SplitLrc(string lrc, SearchSourceEnum searchSource)
        {
            // 换行符统一
            var temp = lrc
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Split('\n');

            var resultList = new List<LyricLineVo>();

            foreach (var line in temp)
            {
                // QQ 音乐歌词正式开始标识符
                if (searchSource == SearchSourceEnum.QQ_MUSIC && "[offset:0]".Equals(line))
                {
                    resultList.Clear();
                    continue;
                }

                var lyricLineVo = new LyricLineVo(line);
                
                // 跳过无效内容
                if (lyricLineVo.IsIllegalContent())
                {
                    continue;
                }

                resultList.Add(lyricLineVo);
            }

            return resultList;
        }

        /**
         * 双语歌词排序
         */
        private static List<LyricLineVo> SortLrc(List<LyricLineVo> originLyrics, List<LyricLineVo> translateLyrics, bool hasOriginLrcPrior)
        {
            int lenA = originLyrics.Count, lenB = translateLyrics.Count;
            var c = new List<LyricLineVo>();

            int i = 0, j = 0;

            while (i < lenA && j < lenB)
            {
                var compare = Compare(originLyrics[i], translateLyrics[j], hasOriginLrcPrior);
                
                if (compare > 0)
                {
                    c.Add(translateLyrics[j++]);
                }
                else if (compare < 0)
                {
                    c.Add(originLyrics[i++]);
                }
                else
                {
                    c.Add(hasOriginLrcPrior ? originLyrics[i++] : translateLyrics[j++]);
                }
            }

            while (i < lenA)
                c.Add(originLyrics[i++]);
            while (j < lenB)
                c.Add(translateLyrics[j++]);
            return c;
        }

        /**
         * 双语歌词合并
         */
        private static List<LyricLineVo> MergeLrc(List<LyricLineVo> originList, List<LyricLineVo> translateList, string splitStr, bool hasOriginLrcPrior)
        {
            var c = SortLrc(originList, translateList, hasOriginLrcPrior);
            
            var list = new List<LyricLineVo>
            {
                c[0]
            };

            for (var i = 1; i < c.Count; i++)
            {
                if (c[i - 1].Timestamp.TimeOffset == c[i].Timestamp.TimeOffset)
                {
                    var index = list.Count - 1;

                    list[index].Content = list[index].Content + splitStr + c[i].Content;
                }
                else
                {
                    list.Add(c[i]);
                }
            }

            return list;
        }

        /**
         * 译文缺省逻辑处理
         */
        private static List<LyricLineVo> DealTranslateLyricDefaultRule(List<LyricLineVo> voList, TranslateLyricDefaultRuleEnum rule)
        {
            if (rule != TranslateLyricDefaultRuleEnum.IGNORE)
            {
                var timestampCounts = new Dictionary<long, int>();

                foreach (var key in voList.Select(one => one.Timestamp.TimeOffset))
                {
                    if (timestampCounts.ContainsKey(key))
                    {
                        timestampCounts[key] += 1;
                    }
                    else
                    {
                        timestampCounts.Add(key,1);
                    }
                }

                var dealTimestamps = (from pair in timestampCounts where pair.Value == 1 select pair.Key).ToList();
                dealTimestamps.Sort((a, b) => a.CompareTo(b));

                var dealTimestampIndex = 0;
                
                for (var i = 0; i < voList.Count; i++)
                {
                    var cur = voList[i];
                    var timestamp = dealTimestamps[dealTimestampIndex];

                    if (cur.Timestamp.TimeOffset == timestamp)
                    {
                        var content = rule == TranslateLyricDefaultRuleEnum.FILL_ORIGIN ? cur.Content : "";
                        voList.Insert(++i, new LyricLineVo(content, cur.Timestamp));
                        dealTimestampIndex++;

                        if (dealTimestampIndex >= dealTimestamps.Count)
                        {
                            break;
                        }
                    }
                }
            }
            
            return voList;
        }
        
        /**
         * 歌词排序函数
         */
        private static int Compare(LyricLineVo originLrc, LyricLineVo translateLrc, bool hasOriginLrcPrior)
        {
            var compareTo = originLrc.CompareTo(translateLrc);

            if (compareTo == 0)
            {
                return hasOriginLrcPrior ? -1 : 1;
            }

            return compareTo;
        }
    }
}