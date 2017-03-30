using System;
using System.Collections.Generic;
using System.Threading;

namespace Tvmaid
{
	internal class EpgQueue
	{
		public bool Enable;

		private List<Service> list = new List<Service>();

		public int Count
		{
			get
			{
				return this.list.Count;
			}
		}

		public void Clear()
		{
			List<Service> obj = this.list;
			lock (obj)
			{
				this.list.Clear();
				this.Enable = false;
			}
		}

		public void Enqueue()
		{
			List<Service> obj = this.list;
			lock (obj)
			{
				this.list.Clear();
				using (Sql sql = new Sql(true))
				{
					string nids = MainDef.GetInstance()["epg.basic"];
					if (nids == "")
					{
						nids = "-1";
					}
					else
					{
						string[] array = nids.Split(new char[]
						{
							','
						});
						string where = "";
						for (int i = 0; i < array.Length; i++)
						{
							string s = "(nid = " + array[i];
							int sid = EpgWait.GetInstance().GetSid(array[i].ToInt());
							if (sid != -1)
							{
								s += " and sid = ";
								s += sid;
							}
							s += ")";
							where += s;
							if (i < (array.Length - 1))
							{
								where += " or ";
							}
						}
						sql.Text = "select *, (fsid >> 32) as nid, (fsid & 0xffff) as sid from service where {0} group by nid, driver order by id";
						sql.Text = sql.Text.Formatex(new object[]
						{
							where
						});
//						Log.Write("sql(basic) = [{0}]".Formatex(new object[]{sql.Text}));
						this.AddList(sql, true);
					}

					sql.Text = "select *, (fsid >> 32) as nid, ((fsid >> 16) & 0xffff) as tsid from service where nid not in ({0}) group by tsid, driver order by id";
					sql.Text = sql.Text.Formatex(new object[]
					{
						nids
					});
//					Log.Write("sql(ex) = [{0}]".Formatex(new object[]{sql.Text}));
					this.AddList(sql, false);
				}
				this.Enable = (this.list.Count > 0);
			}
		}

		private void AddList(Sql sql, bool epgNid)
		{
			using (DataTable table = sql.GetTable())
			{
				while (table.Read())
				{
					Service service = new Service(table);
					service.EpgBasic = epgNid;
					this.list.Add(service);
				}
			}
		}

		public void Enqueue(Service service)
		{
			List<Service> obj = this.list;
			lock (obj)
			{
				this.list.Add(service);
			}
			this.Enable = true;
		}

		public Service Peek(Tuner tuner)
		{
			List<Service> obj = this.list;
			Service result;
			lock (obj)
			{
				if (this.list.Count == 0)
				{
					result = null;
				}
				else
				{
					Service service = null;
					foreach (Service current in this.list)
					{
						if (current.Driver == tuner.Driver)
						{
							service = current;
							break;
						}
					}
					result = service;
				}
			}
			return result;
		}

		public Service Dequeue(Tuner tuner)
		{
			List<Service> obj = this.list;
			bool flag = false;
			Service service2;
			try
			{
				Monitor.Enter(obj, ref flag);
				Service service = this.Peek(tuner);
				if (service != null)
				{
					this.list.RemoveAll((Service s) => s.Fsid == service.Fsid);
				}
				service2 = service;
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(obj);
				}
			}
			return service2;
		}
	}
}
