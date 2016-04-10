package com.shusheng.service;

public class UserService1 {

	/**
	 * 检查用户登录信息
	 * 用户名密码正确时返回0
	 * 用户名不存在返回1
	 * 用户名正确但密码错误返回2 
	 * @param username
	 * @param password
	 * @return
	 */
	public int doLogin(String username, String password) {
		
//		return 0;//正确
//		return 1;//用户名不存在
		return 2;//密码错误
	}

	/**
	 * 用户注册，判断用户名是否存在于数据库，如果username存在于数据库，返回1；
	 * 如果username不存在于数据库，就将用户名密码存入数据库，且返回0
	 * @param username
	 * @param password
	 * @return
	 */
	public int doRegister(String username,String password,int userStatus) {
		return 0;//表示将用户名密码存入数据库
//		return 1;//表示用户名与数据库的重复
	}

}
