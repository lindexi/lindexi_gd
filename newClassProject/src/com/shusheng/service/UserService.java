package com.shusheng.service;

import java.util.List;

import com.shusheng.model.FocusMsg;
import com.shusheng.model.Student;
import com.shusheng.model.Subject;
import com.shusheng.model.Teacher;

public interface UserService {

	/**
	 * 用户登录，检查用户名密码是否正确
	 * 
	 * @param username
	 *            用户名
	 * @param password
	 *            密码
	 * @return 返回0表示登录成功，返回1表示用户名不存在，返回2表示密码错误
	 */
	public int doLogin(String username, String password, int status);

	/**
	 * 用户注册
	 * 
	 * @param username
	 *            用户名
	 * @param password
	 *            密码
	 * @param userStatus
	 *            用户身份
	 * @return
	 */
	public boolean doRegister(String username, String password, int userStatus);

	/**
	 * 更新一个教师
	 * @param teacher
	 */
	public void update(Teacher teacher);
	
	/**
	 * 更新一个学生
	 * @param student
	 */
	public void update(Student student);
	
	/**
	 * 存储关注的科目信息
	 * @param focusMsgs
	 */
	public void saveFocusMsg(List<FocusMsg> focusMsgs);

	/**
	 * 获取到对应的用户
	 * @param username
	 * @param password
	 * @param status
	 * @return 
	 */
	public Object getUser(String username, String password, int status);

}
