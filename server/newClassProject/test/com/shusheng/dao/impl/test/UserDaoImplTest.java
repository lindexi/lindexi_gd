package com.shusheng.dao.impl.test;

import javax.annotation.Resource;

import junit.framework.Assert;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.junit4.SpringJUnit4ClassRunner;

import com.shusheng.dao.UserDao;
import com.shusheng.dao.impl.UserDaoImpl;

@RunWith(SpringJUnit4ClassRunner.class)
@ContextConfiguration(locations = "classpath:resources/beans.xml")
public class UserDaoImplTest {

	@Resource
	private UserDao userDaoImpl;

	@Test
	public void testDoLogin() {//0表示登录成功，1表示用户名不存在，2表示密码错误
		String username = "aa";
		String password = "aaaa";
		int status = 1;
		Assert.assertEquals(1, userDaoImpl.doLogin(username, password, status));
	}

	
	public void testDoRegister(){
		String username = "aa";
		String password = "aaaa";
		
		int userStatus = 0;//学生
		Assert.assertEquals(true, userDaoImpl.doRegister(username, password, userStatus));
		Assert.assertEquals(false, userDaoImpl.doRegister(username, password, userStatus));
		
		userStatus = 1;//教师
		Assert.assertEquals(true, userDaoImpl.doRegister(username, password, userStatus));
		Assert.assertEquals(false, userDaoImpl.doRegister(username, password, userStatus));
	}
}
