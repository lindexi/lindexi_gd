package com.shusheng.dao;

import java.util.List;

import com.shusheng.model.FocusMsg;
import com.shusheng.model.Subject;

public interface UserDao {
	
	/**
	 * 处理用户登录请求
	 * @param username
	 * @param password
	 * @return 返回0表示登录成功，返回1表示用户名不存在，返回2表示密码错误
	 */
	public int doLogin(String username, String password,int status);

	/**
	 * 处理用户注册
	 * @param username
	 * @param password
	 * @param userStatus
	 * @return 返回0表示登录成功，返回1表示用户名不存在，返回2表示密码错误
	 */
	public boolean doRegister(String username, String password, int userStatus);

	/**
	 * 持久化学生信息
	 * @param studentInfo
	 */
	public void update(Object object);

	/**
	 * 存储关注科目的信息
	 * @param focusMsgs
	 */
	public void saveFocusMsg(List<FocusMsg> focusMsgs);

	/**
	 * 获取到一个用户
	 * @param username
	 * @param password
	 * @param status
	 * @return
	 */
	public Object getUser(String username, String password, int status);

}
