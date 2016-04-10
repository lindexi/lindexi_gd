package com.shusheng.service.impl;

import java.util.List;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

import com.shusheng.dao.UserDao;
import com.shusheng.model.FocusMsg;
import com.shusheng.model.Student;
import com.shusheng.model.Subject;
import com.shusheng.model.Teacher;
import com.shusheng.service.UserService;

@Component("userService")
public class UserServiceImpl implements UserService{

	@Autowired
	private UserDao userDao;
	/**
	 * 用户登录
	 * 返回0表示登录成功，返回1表示用户名不存在，返回2表示密码错误
	 */
	@Override
	public int doLogin(String username, String password,int status) {
		return userDao.doLogin(username, password,status);
		
	}

	/**
	 * 用户注册
	 * 返回true表示注册成功，返回false表示用户名已经存在
	 */
	@Override
	public boolean doRegister(String username, String password, int userStatus) {
		return userDao.doRegister(username, password, userStatus);
	}

	/**
	 * 更新一个教师
	 */
	@Override
	public void update(Teacher teacher) {
		userDao.update(teacher);
		
	}

	/**
	 * 更新一个学生
	 */
	@Override
	public void update(Student student) {
		userDao.update(student);
		
	}

	/**
	 * 存储关注的科目信息
	 */
	@Override
	public void saveFocusMsg(List<FocusMsg> focusMsgs) {
		userDao.saveFocusMsg(focusMsgs);
		
	}

	@Override
	public Object getUser(String username, String password, int status) {
		
		return userDao.getUser(username,password,status);
	}


}
