using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tvmaid
{
	internal class TunerServer
	{
		private enum Api
		{
			Close = 45056,
			GetState,
			GetServices,
			GetEvents,
			SetService,
			StartRec,
			StopRec,
			GetEventTime,
			GetTsStatus,
			GetLogo
		}

		public enum ErrorCode
		{
			NoError,
			CreateShared,
			CreateWindow,
			CreateMutex,
			StartRec,
			StopRec,
			SetService,
			GetEvents,
			GetState,
			GetEnv,
			GetEventTime,
			GetTsStatus,
			OutOfShared
		}

		[Flags]
		public enum SendMessageTimeoutFlags : uint
		{
			SMTO_NORMAL = 0u,
			SMTO_BLOCK = 1u,
			SMTO_ABORTIFHUNG = 2u,
			SMTO_NOTIMEOUTIFNOTHUNG = 8u
		}

		private string driverId;

		private string driverPath;

		private const int timeout = 60000;

		public TunerServer(string driverId, string driverPath)
		{
			this.driverId = driverId;
			this.driverPath = driverPath;
		}

		public bool IsOpen()
		{
			return TunerServer.FindWindow("/tvmaid/win", this.driverId) != IntPtr.Zero;
		}

		private void WaitOpen()
		{
			for (int i = 0; i < 600; i++)
			{
				if (this.IsOpen())
				{
					return;
				}
				Thread.Sleep(100);
			}
			throw new Exception("TVTestの初期化が時間内に終了しませんでした。[原因]TVTestが初期化中にエラーになったか、PCの負荷が高過ぎる等が考えられます。");
		}

		public void Open(bool show)
		{
			if (this.IsOpen())
			{
				return;
			}
			Ticket ticket = new Ticket("/tvmaid/mutex/tvtest/open/" + this.driverId);
			try
			{
				if (!ticket.GetOwner(0))
				{
					ticket = null;
					this.WaitOpen();
				}
				else
				{
					string str = show ? "" : " /nodshow /min /silent";
					Process expr_4A = new Process();
					expr_4A.StartInfo.FileName = MainDef.GetInstance()["tvtest"];
					expr_4A.StartInfo.Arguments = string.Format("/d \"{0}\"" + str, this.driverPath);
					expr_4A.StartInfo.UseShellExecute = false;
					expr_4A.StartInfo.EnvironmentVariables.Add("DriverId", this.driverId);
					expr_4A.Start();
					expr_4A.WaitForInputIdle();
					this.WaitOpen();
				}
			}
			finally
			{
				if (ticket != null)
				{
					ticket.Dispose();
				}
			}
		}

		public void Close()
		{
			if (!this.IsOpen())
			{
				return;
			}
			Ticket ticket = new Ticket("/tvmaid/mutex/tvtest/close/" + this.driverId);
			try
			{
				if (!ticket.GetOwner(0))
				{
					ticket.Dispose();
				}
				else
				{
					this.Call(TunerServer.Api.Close);
					for (int i = 0; i < 600; i++)
					{
						if (!this.IsOpen())
						{
							return;
						}
						Thread.Sleep(100);
					}
					Log.Write("TVTestが時間内に終了しませんでした。これ以降TVTestが操作できないかもしれません。");
				}
			}
			finally
			{
				ticket.Dispose();
			}
		}

		public int GetState()
		{
			return Convert.ToInt32(this.Func(TunerServer.Api.GetState));
		}

		public List<Service> GetServices()
		{
			string[] arg_21_0 = this.Func(TunerServer.Api.GetServices).Split(new char[]
			{
				'\u0001'
			}, StringSplitOptions.RemoveEmptyEntries);
			List<Service> list = new List<Service>();
			string[] array = arg_21_0;
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				try
				{
					string[] array2 = text.Split(new char[]
					{
						'\u0002'
					});
					list.Add(new Service
					{
						Driver = Path.GetFileName(this.driverPath),
						Nid = Convert.ToInt32(array2[0]),
						Tsid = Convert.ToInt32(array2[1]),
						Sid = Convert.ToInt32(array2[2]),
						Name = array2[3]
					});
				}
				catch (Exception ex)
				{
					throw new Exception("サービス情報が不正です。[追加情報] " + ex.Message);
				}
			}
			return list;
		}

		public void SetService(Service service)
		{
			string arg = string.Format("{0}\u0001{1}\u0001{2}\0", service.Nid, service.Tsid, service.Sid);
			this.Call(TunerServer.Api.SetService, arg);
		}

		public Event GetEventTime(Service service, int eid)
		{
			string arg = "{0}\u0001{1}\u0001{2}\u0001{3}\0".Formatex(new object[]
			{
				service.Nid,
				service.Tsid,
				service.Sid,
				eid
			});
			string text = this.Func(TunerServer.Api.GetEventTime, arg);
			Event result;
			try
			{
				string[] array = text.Split(new char[]
				{
					'\u0001'
				}, StringSplitOptions.RemoveEmptyEntries);
				result = new Event
				{
					Start = Convert.ToDateTime(array[0]),
					Duration = Convert.ToInt32(array[1])
				};
			}
			catch (Exception ex)
			{
				throw new Exception("番組情報が不正です。[追加情報] " + ex.Message);
			}
			return result;
		}

		public TsStatus GetTsStatus()
		{
			string text = this.Func(TunerServer.Api.GetTsStatus);
			TsStatus result;
			try
			{
				string[] array = text.Split(new char[]
				{
					'\u0001'
				}, StringSplitOptions.RemoveEmptyEntries);
				result = new TsStatus
				{
					Error = Convert.ToInt32(array[0]),
					Drop = Convert.ToInt32(array[1]),
					Scramble = Convert.ToInt32(array[2])
				};
			}
			catch (Exception ex)
			{
				throw new Exception("TSエラー情報が不正です。[追加情報] " + ex.Message);
			}
			return result;
		}

		public List<Event> GetEvents(Service service)
		{
			string arg = string.Format("{0}\u0001{1}\u0001{2}\0", service.Nid, service.Tsid, service.Sid);
			string arg_49_0 = this.Func(TunerServer.Api.GetEvents, arg);
			List<Event> list = new List<Event>();
			string[] array = arg_49_0.Split(new char[]
			{
				'\u0001'
			}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i];
				try
				{
					string[] array2 = text.Split(new char[]
					{
						'\u0002'
					});
					list.Add(new Event
					{
						Eid = Convert.ToInt32(array2[0]),
						Start = Convert.ToDateTime(array2[1]),
						Duration = Convert.ToInt32(array2[2]),
						Title = array2[3],
						Desc = array2[4],
						LongDesc = array2[5],
						Genre = Convert.ToInt64(array2[6]),
						Fsid = service.Fsid
					});
				}
				catch (Exception ex)
				{
					throw new Exception("番組情報が不正です。[追加情報] " + ex.Message);
				}
			}
			return list;
		}

		public void StartRec(string file)
		{
			string arg = string.Format("{0}\0", file);
			this.Call(TunerServer.Api.StartRec, arg);
		}

		public void GetLogo(Service service, string path)
		{
			string arg = string.Format("{0}\u0001{1}\u0001{2}\u0001{3}\0", new object[]
			{
				service.Nid,
				service.Tsid,
				service.Sid,
				path
			});
			this.Call(TunerServer.Api.GetLogo, arg);
		}

		public void StopRec()
		{
			this.Call(TunerServer.Api.StopRec, null);
		}

		private void Call(TunerServer.Api api, string arg)
		{
			this.Func(api, arg, false);
		}

		private void Call(TunerServer.Api api)
		{
			this.Func(api, null, false);
		}

		private string Func(TunerServer.Api api, string arg)
		{
			return this.Func(api, arg, true);
		}

		private string Func(TunerServer.Api api)
		{
			return this.Func(api, null, true);
		}

		private string Func(TunerServer.Api api, string arg, bool isRet)
		{
			IntPtr intPtr = TunerServer.FindWindow("/tvmaid/win", this.driverId);
			if (intPtr == IntPtr.Zero)
			{
				throw new Exception("TVTest呼び出しに失敗しました(TVTestが終了されました)。");
			}
			Ticket ticket = new Ticket("/tvmaid/mutex/call/" + this.driverId);
			Shared shared = null;
			Shared shared2 = null;
			string result;
			try
			{
				if (!ticket.GetOwner(60000))
				{
					throw new Exception("TVTest呼び出しに失敗しました(タイムアウト)。");
				}
				if (arg != null)
				{
					shared = new Shared("/tvmaid/map/in/" + this.driverId);
					shared.Write(arg);
				}
				if (isRet)
				{
					shared2 = new Shared("/tvmaid/map/out/" + this.driverId);
				}
				UIntPtr uIntPtr;
				if (TunerServer.SendMessageTimeout(intPtr, (uint)api, UIntPtr.Zero, IntPtr.Zero, 0u, 60000u, out uIntPtr).ToInt32() == 0)
				{
					throw new Exception("TVTest呼び出しに失敗しました(タイムアウト)。");
				}
				uint num = uIntPtr.ToUInt32();
				if (num != 0u)
				{
					throw new TunerServerExceotion(num);
				}
				result = (isRet ? shared2.Read() : null);
			}
			finally
			{
				if (shared != null)
				{
					shared.Dispose();
				}
				if (shared2 != null)
				{
					shared2.Dispose();
				}
				ticket.Dispose();
			}
			return result;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
	}
}
