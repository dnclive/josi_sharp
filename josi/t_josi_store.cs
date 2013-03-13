using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using fastJSON;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;
using System.Collections;
using josi;
using Community.CsharpSqlite;
using Community.CsharpSqlite.SQLiteClient;
using tlib;

namespace josi
{

	public class t_store
	{

		//врем€ в милесекундах начала выполнени€ запроса к серверу
		TimeSpan josi_store_query_start;

		HttpWebRequest last_request;

		string cookie = "";//"kvl.1.tab_session_log.id=1320";// "kvl.1.tab_session_log.id=1320; path=/";

		string req_uri = "http://kibicom.com/order_store_33/order_store/index.php?";

		t auth_args;

		public t_store()
		{

		}
		/*
		public t_josi_store(t_josi_auth_args args)
		{
			//auth_args=args;
			f_login(args);
		}
		*/

		public t_store(t args)
		{
			auth_args = args;
			f_login(args);
		}

		#region работа с базой

		

		#region запросы к josi

		//выполн€ет запрос получени€/созранени€ данных в josi store
		public void f_store(t args)
		{
			string josi_store_get_put_query = "kvl.0.f={store_get_struct,store_put_struct}" + args["res_dot_key_query_str"].f_str();
			args["query_str"] = new t(josi_store_get_put_query);

			if (args["cancel_prev"].f_bool())
			{
				long now_utc = DateTime.Now.ToFileTimeUtc();
				josi_store_query_start = new TimeSpan(now_utc);
				args["query_start"] = new t(new TimeSpan(now_utc));
			}

			f_get_store_data(args);

			return;

		}


		//выполн€ет произвольные запрос к josi
		public void f_query(t args)
		{
			args["query_str"] = new t(args["dot_key_query_str"].f_str());

			if (args["cancel_prev"].f_val<bool>())
			{
				long now_utc = DateTime.Now.ToFileTimeUtc();
				josi_store_query_start = new TimeSpan(now_utc);
				args["query_start"] = new t(new TimeSpan(now_utc));
			}

			f_get_store_data(args);

			return;

		}


		//выполн€ет запрос генерации id дл€ указанного ресурса josi store
		public void f_gen_id(t args)
		{
			string res_dot_key_query_str = "kvl.0.f=gen_id&kvl.1.tab=" + args["res_name"].f_str() + "&kvl.1.id=" + args["id_key"].f_str();

			f_query(new t()
			{
				{"res_dot_key_query_str", res_dot_key_query_str},
				{
					"f_done",
					new t_f<t,t>(delegate(t args1)
					{
						args["id_str"] = new t(f_get_val_from_json_obj(args1["resp_json"], "tab_customer.id"));
						args["id_int"] = new t(Convert.ToInt32(args["id_str"].f_str()));

						t_uti.f_fdone(args);

						//args.fdone(args);

						//josi_f_done fdone = (t_josi_store_id_gen_args)args;

						//fdone(args);
						return null;
					})
				},
				{"encode_json",true},
				{"cancel_prev",false},
			});

			//string resp_str = f_get_store_data("kvl.0.f=gen_id&kvl.1.tab=tab_customer&kvl.1.id=id");
			//"&kvl.1.tab_customer.discount_calc");

			//Dictionary<string, object> json_obj = (Dictionary<string, object>)JsonParser.JsonDecode(resp_str);

			//MessageBox.Show(resp_str);



			//MessageBox.Show(edit_item_id);

			return;
		}


		//выпон€ет авторизацию пользовател€ в josi
		private bool f_login(t args)
		{
			//если текущее подключение еще не авторизовано то пробуем авторизоватьс€
			if (!args["authenticated"].f_val<bool>())
			{
				//формируем запрос на авторизацию
				string res_dot_key_query_str = "kvl.0.f=login&kvl.0.login=" + args["login_name"].f_str() + "&kvl.0.pass=" + args["pass"].f_str();

				//выполн€ем запрос авторизации
				f_query(new t()
				{
					{"res_dot_key_query_str",res_dot_key_query_str},
					{
						"f_done",
						new t_f<t,t>(delegate(t args1)
						{
							//MessageBox.Show(args1.resp_str);

							args["tab_login"] = new t()
							{
								{"id",f_get_val_from_json_obj(args1["resp_json"].f_val(), "session.tab_login.id").ToString()},
								{"login",f_get_val_from_json_obj(args1["resp_json"].f_val(), "session.tab_login.login").ToString()},
								{"tab_session",new t()
									{
										{"id", f_get_val_from_json_obj(args1["resp_json"].f_val(), "session.tab_login.tab_session.id").ToString()}
									}
								}
							};

							if (args["tab_login"]["login"].f_str() == args["login_name"].f_str())
							{
								args["authenticated"] = new t(true);
							}
							else
							{
								//josi_msg_box.fshow("ќтказ в авторизации. ќбратитесь к администратору!", "ќ ", "–едактировать");

							}

							t_uti.f_fdone(args1);
							return null;
						})
					},
					{"encode_json",true},
					{"cancel_prev",false}
				});
			}



			//если авторизаци€ успешна возвращаем true;
			return true;

		}


		#endregion


		private void f_get_store_data(t args)
		{

			string req_srt = req_uri + args["query_str"].f_str();

			//MessageBox.Show(req_url);

			//наш http запрос к серверу формируетс€ из адреса дл€ запросов и предаваемого запроса query_str
			args["req"] = new t((HttpWebRequest)WebRequest.Create(req_srt));

			if (1 == 0)
			{

				//req.BeginGetResponse(f_get_response, args.f_mix(req));


				//MessageBox.Show("1");

				//return "";
			}
			else
			{
				//Converter
				// передаем cookie, полученные в предыдущем запросе
				if (!String.IsNullOrEmpty(cookie))
				{
					args["req"].f_val<HttpWebRequest>().Headers.Add(HttpRequestHeader.Cookie, cookie);
				}

				//таймаут запроса
				args["req"].f_val<HttpWebRequest>().Timeout = 10000;

				//принимать любые сертификаты
				System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

				//делегат вызова функции отправки запроса на сервер и получени€ ответа
				//hwr_delegate hwr_dlg = new hwr_delegate(f_get_response);

				//t_f<t> t_f_get_resp = new t_f(f_get_response_dev);

				t_f<t> d = delegate()
				{
					return new t();
				};

				IAsyncResult ar = (new t_f(delegate()
				{
					//t_josi_store_req_args args = (t_josi_store_req_args)res.AsyncState;

					//MessageBox.Show(args.query_start.Ticks.ToString());

					//MessageBox.Show(josi_store_query_start.Ticks.ToString());

					

					HttpWebResponse response;
					try
					{
						//MessageBox.Show("1");
						response = (HttpWebResponse)args["req"].f_val<HttpWebRequest>().GetResponse();

						if (args["cancel_prev"].f_val<bool>() && args["query_start"].f_val<TimeSpan>().CompareTo(josi_store_query_start) != 0)
						{
							//MessageBox.Show("123");
							response.Close();

							//t_uti.f_fdone(args);
							return;
						}

						//если сервер прислал новый cookie то берем его иначе оставл€ем тот что хранитс€ на текущий момент
						cookie = String.IsNullOrEmpty(response.Headers["Set-Cookie"]) ? cookie : response.Headers["Set-Cookie"];

						//MessageBox.Show(cookie);
						//richTextBox1.Text = cookie;

						Stream dataStream = response.GetResponseStream();
						StreamReader reader = new StreamReader(dataStream);
						string resp_str = reader.ReadToEnd();

						args["resp_str"] = new t(resp_str);

						//if (args.encode_json)
						{
							args["resp_json"] = new t((Dictionary<string, object>)JsonParser.JsonDecode(resp_str));
							t_uti.f_fdone(args);
							//(args["fdone"].f)(args);	

						}

						//MessageBox.Show(resp_str);

						reader.Close();
						dataStream.Close();
						response.Close();

						t_uti.f_fdone(args);

					}
					catch (Exception ex)
					{
						//MessageBox.Show(ex.Message);
						//MessageBox.Show("Ќе удалось св€затьс€ с сервером.\n\r" +
						//				"ѕроверьте подключение к интернету.",
						//				"ќшибка св€зи с серевером",
						//				MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
					return;
				})).BeginInvoke(new AsyncCallback(delegate(IAsyncResult iar)
				{
					// Retrieve the delegate.

					AsyncResult result = (AsyncResult)iar;
					t_f caller = (t_f)result.AsyncDelegate;

					// Call EndInvoke to retrieve the results.
					caller.EndInvoke(iar);
				}), null);

				return;
			}
		}


		#endregion работа с базой

		#region json_dot_val

		private object f_get_val_from_json_obj(object json_obj, string dot_key)
		{
			//Dictionary<string, object> json_obj_dic=null;
			//ArrayList json_obj_arr=null;

			string dot_keyi = f_get_top_dot_keyi(dot_key);
			string dot_key_tail = f_get_dot_key_tail(dot_key);

			if (dot_keyi == "")
			{
				return json_obj;
			}

			int num_dot_keyi;
			if (int.TryParse(dot_keyi, out num_dot_keyi))
			{
				ArrayList json_obj_arr = (ArrayList)json_obj;



				return f_get_val_from_json_obj(json_obj_arr[num_dot_keyi], dot_key_tail);

				/*
				foreach (object json_obj_arri in json_obj_arr)
				{
					f_get_val_from_json_obj(json_obj_arri, dot_key_tail);	
				}
				*/
			}
			else
			{

				Dictionary<string, object> json_obj_dic = (Dictionary<string, object>)json_obj;

				return f_get_val_from_json_obj(json_obj_dic[dot_keyi], dot_key_tail);
			}
			return null;
		}

		private string f_get_top_dot_keyi(string dot_key)
		{
			int doti = dot_key.IndexOf('.');
			return doti < 0 ? dot_key : dot_key.Substring(0, dot_key.IndexOf('.'));
		}

		private string f_get_dot_key_tail(string dot_key)
		{
			int doti = dot_key.IndexOf('.');
			return doti < 0 ? "" : dot_key.Substring(doti + 1, dot_key.Length - doti - 1);
		}


		#endregion json_dot_val
	}

	class local
	{
		public local(string db_file_name)
		{

			SqliteConnection con = new SqliteConnection();
		}
	}

}
