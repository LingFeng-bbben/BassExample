//下载dll：https://www.un4seen.com/
//注意根据平台的不同 需要的dll也不同
//需要bass.dll
using ManagedBass;
//需要bass_fx.dll
using ManagedBass.Fx;
using System.IO;
using System.Text;

namespace BassExample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Bass.Init();
            //解码音频
            var decodeStream = Bass.CreateStream("track.mp3", Flags: BassFlags.Prescan | BassFlags.Decode);

            var bytelength = Bass.ChannelGetLength(decodeStream);
            var length = Bass.ChannelBytes2Seconds(decodeStream, bytelength);
            Console.WriteLine("音频总长：" + length);

            //设置解码位置
            Bass.ChannelSetPosition(decodeStream, 114514, PositionFlags.Decode | PositionFlags.Bytes);

            Console.WriteLine("RAWDATA----------------");

            //获取原始数据
            for (int i = 0; i < 10; i++)
            {
                byte[] bytes = new byte[16];
                Bass.ChannelGetData(decodeStream, bytes, bytes.Length);
                foreach(var b in bytes)
                {
                    //奇数是左声道 偶数是右声道（应该）
                    Console.Write(b + ",");
                }
                //注意 使用channel get data每调用一次会让内部解码位置前移
                var bytePosition = Bass.ChannelGetPosition(decodeStream, PositionFlags.Decode | PositionFlags.Bytes);
                Console.WriteLine("解码位置：" + bytePosition);
            }

            //重置解码位置
            Bass.ChannelSetPosition(decodeStream, 114514, PositionFlags.Decode | PositionFlags.Bytes);

            Console.WriteLine("LEVELS----------------");

            //获取声音大小 （已经归一化）
            for (int i=0; i < 10; i++)
            {
                //loword左声道 hiword右声道
                var level= (double)BitHelper.LoWord(Bass.ChannelGetLevel(decodeStream)) / 32768;
                Console.WriteLine(level);
            }

            Bass.ChannelSetPosition(decodeStream, 0, PositionFlags.Decode | PositionFlags.Bytes);

            Console.ReadLine();

            //解码后的音频送入变速效果器
            var tempoStream = BassFx.TempoCreate(decodeStream, BassFlags.Default);
            //设置播放位置
            Bass.ChannelSetPosition(tempoStream, Bass.ChannelSeconds2Bytes(tempoStream, 10));
            Bass.ChannelPlay(tempoStream);
            for(int i = 0;i<1145;i++)
            {
                Console.Write('\r');
                //获取播放位置
                var second = Bass.ChannelBytes2Seconds(tempoStream, Bass.ChannelGetPosition(tempoStream));
                var level = (double)BitHelper.LoWord(Bass.ChannelGetLevel(tempoStream)) / 32768;
                var barcount = (int)(level * 50);
                for(int j = 0; j < 50; j++)
                {
                    if (j < barcount)
                    {
                        Console.Write("■");
                    }
                    else
                    {
                        Console.Write("□");
                    }
                }
                Console.Write(" 播放位置: " + second + "    音量：" + level);
                await Task.Delay(10);
                if (i == 514)
                {
                    //设置倍速
                    var playspeed = 0.8;
                    Bass.ChannelSetAttribute(tempoStream, ChannelAttribute.Tempo, (playspeed - 1) * 100f);
                }
            }

            Console.WriteLine("\n暂停");
            Bass.ChannelPause(tempoStream);
            await Task.Delay(1000);
            Bass.ChannelSetAttribute(tempoStream, ChannelAttribute.Tempo, (1 - 1) * 100f);
            Console.WriteLine("\n播放...");
            Bass.ChannelPlay(tempoStream);
            await Task.Delay(10000);

            //销毁对象（很重要！！！！！！）
            Bass.ChannelStop(decodeStream);
            Bass.StreamFree(decodeStream);
            Bass.ChannelStop(tempoStream);
            Bass.StreamFree(tempoStream);

            //如果不需要变速：(给各类效果音用的话)
            var stream = Bass.CreateStream("track.mp3", Flags: BassFlags.Prescan);
            Bass.ChannelPlay(stream, true);
            await Task.Delay((int)(length*1000));
            Bass.ChannelStop(stream);
            Bass.StreamFree(stream);

            //获取报错
            Console.WriteLine(Bass.LastError);

            
        }
    }
}