using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Tvmaid
{
	internal class Tuner
	{
		public enum State
		{
			View,
			Recoding,
			Paused,
			Stoped,
			Unknown
		}

		private TunerServer server;

		private bool isStart;

		public int Id;

		public string Name;

		public string DriverPath;

		public int DriverIndex;

		public string Driver
		{
			get
			{
				return Path.GetFileName(this.DriverPath);
			}
		}

		public string DriverId
		{
			get
			{
				return "{0}/{1}".Formatex(new object[]
				{
					this.Driver,
					this.DriverIndex
				});
			}
		}

		public Tuner(string name, string driverPath)
		{
			this.Id = -1;
			this.Name = name;
			this.DriverPath = driverPath;
			this.DriverIndex = 0;
			this.server = new TunerServer(this.DriverId, this.DriverPath);
		}

		public Tuner(DataTable t)
		{
			this.Init(t);
		}

		private void Init(DataTable t)
		{
			this.Id = t.GetInt("id");
			this.Name = t.GetStr("name");
			this.DriverPath = t.GetStr("driver_path");
			this.DriverIndex = t.GetInt("driver_index");
			this.server = new TunerServer(this.DriverId, this.DriverPath);
		}

		public Tuner(Sql sql, string name)
		{
			sql.Text = "select * from tuner where name = '{0}'".Formatex(new object[]
			{
				Sql.SqlEncode(name)
			});
			using (DataTable table = sql.GetTable())
			{
				if (!table.Read())
				{
					throw new Exception("チューナがありません。" + name);
				}
				this.Init(table);
			}
		}

		public bool IsStart()
		{
			return this.isStart;
		}

		public bool IsOpen()
		{
			return this.server.IsOpen();
		}

		public void Open(bool show = false)
		{
			if (!this.IsOpen())
			{
				this.isStart = true;
				this.server.Open(show);
			}
		}

		public void Close()
		{
			this.CheckRecording();
			this.isStart = false;
			this.server.Close();
		}

		public Tuner.State GetState()
		{
			Tuner.State result;
			try
			{
				if (this.IsOpen())
				{
					result = (Tuner.State)this.server.GetState();
				}
				else
				{
					result = Tuner.State.Stoped;
				}
			}
			catch
			{
				result = Tuner.State.Unknown;
			}
			return result;
		}

		public void SetService(Service service)
		{
			try
			{
				this.CheckRecording();
				this.server.SetService(service);
			}
			catch (TunerServerExceotion arg_14_0)
			{
				if (arg_14_0.Code != 6u)
				{
					throw;
				}
				Thread.Sleep(1000);
				this.server.SetService(service);
			}
		}

		private void CheckRecording()
		{
			Tuner.State state = this.GetState();
			if (state == Tuner.State.Recoding || state == Tuner.State.Paused)
			{
				throw new Exception("録画中です。");
			}
		}

		public void GetLogo(Service service, string path)
		{
			this.server.GetLogo(service, path);
		}

		public Event GetEventTime(Service service, int eid)
		{
			return this.server.GetEventTime(service, eid);
		}

		public TsStatus GetTsStatus()
		{
			return this.server.GetTsStatus();
		}

		public void StartRec(string file)
		{
			this.server.StartRec(file);
		}

		public void StopRec()
		{
			this.server.StopRec();
		}

		public void Add(Sql sql)
		{
			sql.BeginTrans();
			try
			{
				this._Add(sql);
			}
			finally
			{
				sql.Commit();
			}
		}

		private void _Add(Sql sql)
		{
			if (this.Id == -1)
			{
				this.Id = sql.GetNextId("tuner");
			}
			sql.Text = "select count(id) from tuner where driver = '" + Sql.SqlEncode(this.Driver) + "'";
			object data = sql.GetData();
			this.DriverIndex = (int)((long)data);
			sql.Text = "insert into tuner values({0}, '{1}', '{2}', '{3}', {4});".Formatex(new object[]
			{
				this.Id,
				Sql.SqlEncode(this.Name),
				Sql.SqlEncode(this.DriverPath),
				Sql.SqlEncode(this.Driver),
				this.DriverIndex
			});
			sql.Execute();
		}

		public void GetEvents(Sql sql, Service service)
		{
			List<Event> list = null;
			try
			{
				list = this.server.GetEvents(service);
			}
			catch (TunerServerExceotion arg_11_0)
			{
				Log.Write(arg_11_0.Message);
				return;
			}
			try
			{
				sql.BeginTrans();
				foreach (Event current in list)
				{
					sql.Text = "select id from event where eid = {0} and fsid = {1}".Formatex(new object[]
					{
						current.Eid,
						current.Fsid
					});
					object data = sql.GetData();
					if (data == null)
					{
						current.Id = -1;
					}
					else
					{
						current.Id = (int)((long)data);
					}
					current.Add(sql);
				}
			}
			finally
			{
				sql.Commit();
			}
		}

		public static void Update(Sql sql)
		{
			PairList expr_0F = new PairList(Util.GetUserPath("tuner.def"));
			expr_0F.Load();
			sql.Text = "delete from tuner";
			sql.Execute();
			foreach (KeyValuePair<string, string> current in expr_0F)
			{
				new Tuner(current.Key, current.Value).Add(sql);
			}
		}

		public void GetServices(Sql sql)
		{
			List<Service> services;
			try
			{
				this.Open(false);
				services = this.server.GetServices();
			}
			finally
			{
				if (this.IsOpen())
				{
					this.Close();
				}
			}
			bool flag = false;
			try
			{
				sql.BeginTrans();
				foreach (Service current in services)
				{
					if (current.Fsid != 0L)
					{
						sql.Text = "select id from service where driver = '{0}' and fsid = {1}".Formatex(new object[]
						{
							Sql.SqlEncode(current.Driver),
							current.Fsid
						});
						if (sql.GetData() == null)
						{
							current.Add(sql);
						}
						else
						{
							flag = true;
						}
					}
				}
				sql.Commit();
			}
			catch
			{
				sql.Rollback();
				throw;
			}
			if (flag)
			{
				throw new DupServiceException("");
			}
		}
	}
}
