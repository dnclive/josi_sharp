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
//using josi;
//using Community.CsharpSqlite;
//using Community.CsharpSqlite.SQLiteClient;
using kibicom.tlib;
using System.Data;

namespace kibicom.josi
{

	public class t_store:t
	{

		//время в милесекундах начала выполнения запроса к серверу
		TimeSpan josi_store_query_start;

		HttpWebRequest last_request;

		string cookie = "";//"kvl.1.tab_session_log.id=1320";// "kvl.1.tab_session_log.id=1320; path=/";

		string req_uri = "http://kibicom.com/order_store_33/order_store/index.php";

		t auth_args;

		public t_store()
		{
			this["josi_end_point"] = new t("http://kibicom.com/order_store_33/order_store/index.php");
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
			this["josi_end_point"] = args["josi_end_point"];
			this["req_timeout"] = args["req_timeout"].f_def(10000);
			if (args["login_on_cre"].f_val<bool>())
			{
				f_login(args);
			}
		}

		#region on_needs

		private event t_f<t, t> on_needs_done;

		//функция которую вызывают, когда выполнено что то 
		//что может быть needs для вызывающего кода
		private t f_raise_on_needs_done(t args)
		{
			int i = 0;
			while (i<this["on_needs"].Count)
			//foreach (t need in (IList<t>)this["on_needs"])
			{
				t need = this["on_needs"][i];
				bool is_needs_done = true;
				foreach (t f_need in (IList<t>)need["f_needs"])
				{
					//проверяем текущие значения по требуемым ключам
					//по умолчанию считаем что false
					is_needs_done &= this[f_need.f_str()].f_def(false).f_val<bool>();
				}

				//если для данной функции все потребоности удовлетварены
				//вызывае ее
				if (is_needs_done)
				{
					t.f_f("f_needs_done", need["f_args"]);
					this["on_needs"].f_drop(need);
				}
				else
				{
					i++;
				}
				
			}

			return new t();

			if (on_needs_done != null)
			{
				on_needs_done(args);
			}

			
		}

		//ставить задачи  в очередь на выполнение
		//когда будут выполнены все необходимые условия
		private t f_check_all_needs(t args)
		{
			t needs = args["needs"].f_def(null);

			//если нет никаких требований
			//просто выполняем функцию
			if (needs.Count == 0)
			{
				t.f_f("f_needs_done", args);
				return new t();
			}

			t_f<t, t> f_needs_done = args["f_needs_done"].f_f();

			//создаем новый callback
			this["on_needs"].Add(new t()
			{
				{"f",f_needs_done},
				{"f_args",args},
				{"f_needs",needs}
			});

			//проверяем needs
			f_raise_on_needs_done(new t());

			//цепляем событие на needs...
			//on_needs_done += new t_f<t, t>(f_needs_done);

			return new t();
		}

		#endregion on_needs

		#region работа с базой



		#region запросы к josi

		//выполняет запрос получения/созранения данных в josi store
		public t f_store(t args)
		{
			//выполняем запрос когда выполнены все необходимые условия
			//создаем новые аргкументы в них закидываем все из принятых
			//и докидываем f_needs_done
			f_check_all_needs(new t().f_add(true, args).f_add(true, new t()
			{
				{
					"f_needs_done", new t_f<t,t>(delegate(t args1)
					{
						string josi_store_get_put_query = 
							"kvl.0.f={store_get_struct,store_put_struct}" + args["res_dot_key_query_str"].f_str()+
							"&kvl.1.debug_group="+args["debug_group"].f_def(false);
						args["query_str"] = new t(josi_store_get_put_query);

						if (args["cancel_prev"].f_bool())
						{
							long now_utc = DateTime.Now.ToFileTimeUtc();
							josi_store_query_start = new TimeSpan(now_utc);
							args["query_start"] = new t(new TimeSpan(now_utc));
						}

						if (args["method"].f_str().ToLower()=="post")
						{
							args["post_t"].f_def_set("").f_set(new t()
							{
								{
									"kvl", new t()
									{
										new t(){{"key","val"}},
										new t()
										{
											{"tab_arr", args["put_tab_arr"]},
											{"where", args["get_tab_arr"]}
										}
									}
								}
							});
						}

						f_get_store_data(args);

						return new t();
					})
				}
			}));

			return this;

		}

		//выполняет произвольные запрос к josi
		public void f_query(t args)
		{
			//копируем значение query_str
			args["query_str"] = new t(args["res_dot_key_query_str"].f_str());

			//выполняем запрос когда выполнены все необходимые условия
			f_check_all_needs(new t().f_add(true, args).f_add(true, new t()
			{
				{
					"f_needs_done", new t_f<t,t>(delegate(t args1)
					{
						if (args["is_need_auth"].f_def(false).f_bool()&&!this["authenticated"].f_def(false).f_bool())
						{
							t.f_f("f_fail", args);
						}
						if (args["cancel_prev"].f_bool())
						{
							long now_utc = DateTime.Now.ToFileTimeUtc();
							josi_store_query_start = new TimeSpan(now_utc);
							args["query_start"] = new t(new TimeSpan(now_utc));
						}

						//MessageBox.Show(args["query_str"].f_str());

						f_get_store_data(args);

						return new t();
					})
				}
			}));

			return;

		}


		//выполняет запрос генерации id для указанного ресурса josi store
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


		//выпоняет авторизацию пользователя в josi
		public bool f_login(t args)
		{
			int auth_try_count = args["auth_try_count"].f_int();

			//если текущее подключение еще не авторизовано то пробуем авторизоваться
			if (!args["authenticated"].f_val<bool>())
			{
				//формируем запрос на авторизацию
				string res_dot_key_query_str = "kvl.0.f=login&kvl.0.login=" + args["login_name"].f_str() + "&kvl.0.pass=" + args["pass"].f_str();

				//выполняем запрос авторизации
				f_query(new t()
				{
					{"res_dot_key_query_str",res_dot_key_query_str},
					{
						"f_done", new t_f<t,t>(delegate(t args1)
						{
							//MessageBox.Show(f_get_val_from_json_obj(args1["resp_json"].f_val(), "session.tab_login.id").ToString());

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
								this["authenticated"] = new t(true);
								this["is_auth_done"] = new t(true);
								f_f("f_done", args);
							}
							else
							{
								//josi_msg_box.fshow("Отказ в авторизации. Обратитесь к администратору!", "ОК", "Редактировать");
								args["authenticated"] = new t(false);
								this["authenticated"] = new t(false);
								this["is_auth_done"] = new t(true);
								f_f("f_fail", args);
							}

							//t_uti.f_fdone(args);

							//f_f("f_done", args);

							//вызываем событие изменения needs
							f_raise_on_needs_done(new t());

							return null;
						})
					},
					{
						"f_fail", new t_f<t,t>(delegate(t args1)
						{
							args["authenticated"] = new t(false);
							this["authenticated"] = new t(false);
							this["is_auth_done"] = new t(true);
							f_f("f_fail", args);
							
							if (args["auth_try_count_done"].f_def_set(1).f_inc().f_int()<=args["auth_try_count"].f_int())
							{
								f_login(args);
							}

							//вызываем событие изменения needs
							f_raise_on_needs_done(new t());
							return new t();
						})
					},
					{"encode_json",true},
					{"cancel_prev",false}
				});
			}



			//если авторизация успешна возвращаем true;
			return true;

		}


		#endregion


		private void f_get_store_data(t args)
		{
			
			//string req_srt = req_uri + args["query_str"].f_str();
			string req_str = this["josi_end_point"].f_str() + "?" + args["query_str"].f_str();
			
			//timeout соединения
			int timeout = this["req_timeout"].f_def(args["req_timeout"].f_val()).f_def(10000).f_int();

			//метод запроса
			string method = args["method"].f_def("GET").f_str();

			//отправляемые данные в виде объекта t

			t post_t = args["post_t"];

			string post_data_str = args["post_data_str"].f_str();

			if (post_data_str == "" && post_t != null)
			{
				post_data_str = post_t.f_json().f_get("json_str").f_str();
			}

			//MessageBox.Show(post_data_str);

			//timeout = 1000;
			//MessageBox.Show(req_str);

			//наш http запрос к серверу формируется из адреса для запросов и предаваемого запроса query_str
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(req_str);

			args["req"] = new t((HttpWebRequest)req);


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
					req.Headers.Add(HttpRequestHeader.Cookie, cookie);
				}

				//таймаут запроса
				req.Timeout = timeout;

				//принимать любые сертификаты
				System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

				if (method.ToLower() == "post")
				{
					//header
					req.ContentType = "text/plain";

					//метод запроса
					req.Method = method;

					byte[] byteArray = Encoding.UTF8.GetBytes(post_data_str);
					req.ContentLength = byteArray.Length;
					Stream datastream = req.GetRequestStream();
					datastream.Write(byteArray, 0, byteArray.Length);
					datastream.Close();

				}

				//делегат вызова функции отправки запроса на сервер и получения ответа
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

							f_f("f_canceled", args);

							//t_uti.f_fdone(args);
							return;
						}


						if (response.StatusCode != HttpStatusCode.OK)
						{
							f_f("f_fail", args);
						}

						//если сервер прислал новый cookie то берем его иначе оставляем тот что хранится на текущий момент
						cookie = String.IsNullOrEmpty(response.Headers["Set-Cookie"]) ? cookie : response.Headers["Set-Cookie"];

						//MessageBox.Show(cookie);
						//richTextBox1.Text = cookie;

						Stream dataStream = response.GetResponseStream();
						StreamReader reader = new StreamReader(dataStream);
						string resp_str = reader.ReadToEnd();

						args["resp_str"] = new t(resp_str);

						//MessageBox.Show(resp_str);

						//if (args.encode_json)
						{
							args["resp_json"] = new t((Dictionary<string, object>)JsonParser.JsonDecode(resp_str));
							//t_uti.f_fdone(args);

							f_f("f_done", args);

							//(args["fdone"].f)(args);	

						}

						//MessageBox.Show(resp_str);

						reader.Close();
						dataStream.Close();
						response.Close();

						//t_uti.f_fdone(args);

					}
					catch (Exception ex)
					{
						//MessageBox.Show(ex.Message);
						//MessageBox.Show("Не удалось связаться с сервером.\n\r" +
						//				"Проверьте подключение к интернету.",
						//				"Ошибка связи с серевером",
						//				MessageBoxButtons.OK, MessageBoxIcon.Error);
						f_f("f_fail", args);
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

	/*
	class local
	{
		public local(string db_file_name)
		{

			SqliteConnection con = new SqliteConnection();
		}
	}
	*/
}
