package com.shusheng.util;
import java.io.IOException;
import java.io.PrintWriter;

import javax.servlet.http.HttpServletResponse;

import net.sf.json.JSONArray;
import net.sf.json.JSONObject;

public class CommonUtil {

	
	/**
	 * 工具类，发送json数据（不包含list）
	 * @param response
	 * @param object
	 */
	public static void sendJsonObject(HttpServletResponse response,Object object){
		response.setContentType("application/json");
		response.setHeader("Cache-Control", "no-store");
		JSONObject jsonObject = JSONObject.fromObject(object);
		try {
			PrintWriter pw=response.getWriter();
			pw.write(jsonObject.toString());
			pw.flush();
		} catch (IOException e) {
			e.printStackTrace();
		}
	}
	
	/**
	 * 工具类，发送json数据（针对Map）
	 * @param response
	 * @param object
	 */
	public static void sendJsonArray(HttpServletResponse response,Object object){
		response.setContentType("application/json");
		response.setHeader("Cache-Control", "no-store");
		JSONArray jsonObject = JSONArray.fromObject(object);
		try {
			PrintWriter pw=response.getWriter();
			pw.write(jsonObject.toString());
			pw.flush();
			pw.close();
			pw = null;	
		} catch (IOException e) {
			e.printStackTrace();
		}
	}

}
