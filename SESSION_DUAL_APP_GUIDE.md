# Session Management - Dual App Setup (Admin + React User)

## The Problem

You have two separate frontend applications:

- **Admin Dashboard** (Razor/Server-side rendered) - shares same backend
- **React User App** (SPA) - shares same backend

Both share the same `.AspNetCore.Session` cookie from the backend, but they're independent apps that don't know about each other.

### Current Flow Issues:

```
Scenario 1: Login as Admin, then Login as User
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Backend Session State:
  Admin_MemberId = "admin123"  ✅
  User_MemberId = "user456"    ✅  (Now added)
  Both have separate keys = No conflict ✅

Frontend State:
  Admin: localStorage = admin data
  React: localStorage = user data  ✅

Issue on Refresh:
  Admin page refresh → Checks Admin_MemberId → Still exists → Should stay logged in
  React page refresh → Calls /api/member/member-self → Should work ✅

If logout from Admin:
  Backend removes: Admin_MemberId, Admin_MemberNick, etc.
  But keeps: User_MemberId, User_MemberNick, etc.
  React should still work ✅

If logout from React:
  Backend removes: User_MemberId, User_MemberNick, etc.
  But keeps: Admin_MemberId, Admin_MemberNick, etc.
  Admin should still work ✅
```

## Root Cause Analysis

The actual problem is likely:

1. **React not properly handling 401 errors** on refresh
2. **localStorage not being cleared** when session expires
3. **CORS or credentials issue** preventing session cookie from being sent

## Solution

### Step 1: Enhance Your React MemberService

Make sure it properly handles session validation:

```typescript
// MemberService.ts

import axios from "axios";
import { serverApi } from "../libs/config";
import { Member } from "../libs/types/member";

const api = axios.create({
  baseURL: serverApi,
  withCredentials: true, // ✅ Critical for session cookies
});

class MemberService {
  private readonly path = serverApi;

  /**
   * Check if user is still logged in
   * Call this on app load to validate session
   */
  public async validateSession(): Promise<Member | null> {
    try {
      const url = `${this.path}/api/member/member-self`;
      console.log("🔐 Validating session...");

      const result = await api.get(url, {
        withCredentials: true,
        headers: {
          "Cache-Control": "no-cache",
        },
      });

      console.log("✅ Session valid, user:", result.data);
      localStorage.setItem("memberData", JSON.stringify(result.data));
      return result.data;
    } catch (error: any) {
      console.error("❌ Session validation failed:", error.response?.status);

      // Session expired or invalid
      if (error.response?.status === 401) {
        console.log("🔄 Session expired, clearing local data");
        this.clearLocalMember();
        return null;
      }

      throw error;
    }
  }

  public async getMyDetails(): Promise<Member> {
    try {
      const url = `${this.path}/api/member/member-self`;
      console.log("🔐 Fetching member details...");

      const result = await api.get(url, {
        withCredentials: true,
        headers: {
          "Cache-Control": "no-cache",
        },
      });

      console.log("✅ Member details fetched:", result.data);
      localStorage.setItem("memberData", JSON.stringify(result.data));
      return result.data;
    } catch (error: any) {
      console.error("❌ getMyDetails failed:", error.response?.status);

      if (error.response?.status === 401) {
        this.clearLocalMember();
      }

      throw error;
    }
  }

  public async login(
    memberNick: string,
    memberPassword: string
  ): Promise<Member> {
    try {
      const url = `${this.path}/api/member/login`;
      const formData = new FormData();
      formData.append("memberNick", memberNick);
      formData.append("memberPassword", memberPassword);

      console.log("🔐 Attempting login...");

      const result = await api.post(url, formData, {
        withCredentials: true,
      });

      console.log("✅ Login successful:", result.data);
      const member = result.data.member || result.data;
      localStorage.setItem("memberData", JSON.stringify(member));
      return member;
    } catch (error: any) {
      console.error("❌ Login failed:", error.response?.data);
      this.clearLocalMember();
      throw error;
    }
  }

  public async logout(): Promise<void> {
    try {
      const url = `${this.path}/api/member/logout`;
      console.log("🔐 Logging out...");

      await api.post(url, {}, { withCredentials: true });

      console.log("✅ Logout successful");
      this.clearLocalMember();
    } catch (error: any) {
      console.error("❌ Logout failed:", error);
      this.clearLocalMember();
      throw error;
    }
  }

  public async updateMemberProfile(formData: FormData): Promise<Member> {
    try {
      const url = `${this.path}/api/member/update-self`;
      console.log("🔐 Updating profile...");

      const result = await api.post(url, formData, {
        withCredentials: true,
        headers: {
          "Content-Type": "multipart/form-data",
        },
      });

      console.log("✅ Profile updated:", result.data);
      const updated = result.data.data || result.data;
      localStorage.setItem("memberData", JSON.stringify(updated));
      return updated;
    } catch (error: any) {
      console.error("❌ Profile update failed:", error);
      if (error.response?.status === 401) {
        this.clearLocalMember();
      }
      throw error;
    }
  }

  public loadLocalMember(): Member | null {
    const data = localStorage.getItem("memberData");
    try {
      return data ? (JSON.parse(data) as Member) : null;
    } catch (err) {
      console.warn("Invalid localStorage member data");
      return null;
    }
  }

  public clearLocalMember(): void {
    console.log("🗑️ Clearing local member data");
    localStorage.removeItem("memberData");
  }
}

export default new MemberService();
```

### Step 2: Update Your App.tsx to Validate Session on Load

```typescript
// App.tsx

import { useEffect, useState } from "react";
import MemberService from "./services/MemberService";

function App() {
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    // On app load, validate session
    validateUserSession();
  }, []);

  const validateUserSession = async () => {
    try {
      console.log("🔄 App loading - Validating session...");

      // Try to get current user from backend
      const member = await MemberService.validateSession();

      if (member) {
        setIsAuthenticated(true);
        console.log("✅ User is logged in:", member);
      } else {
        setIsAuthenticated(false);
        console.log("❌ User is not logged in");
      }
    } catch (error) {
      console.error("Session validation error:", error);
      setIsAuthenticated(false);
      MemberService.clearLocalMember();
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return <div>Loading...</div>;
  }

  return (
    <div>
      {isAuthenticated ? (
        // Show dashboard
        <Dashboard />
      ) : (
        // Show login
        <Login
          onLoginSuccess={() => {
            validateUserSession();
          }}
        />
      )}
    </div>
  );
}

export default App;
```

### Step 3: Backend - Add Session Commit on Login

Update your login endpoint to explicitly commit the session:

```csharp
// In MemberController.cs - Login endpoint

[HttpPost("login")]
public async Task<IActionResult> Login([FromForm] LoginInput input)
{
    try
    {
        _logger.LogInformation("login");

        var result = await _memberService.LoginAsync(input);

        // Store auth session
        HttpContext.Session.SetString("User_MemberId", result.Id);
        HttpContext.Session.SetString("User_MemberNick", result.MemberNick);
        HttpContext.Session.SetString("User_MemberType", result.MemberType.ToString());
        HttpContext.Session.SetString("User_MemberImage", result.MemberImage ?? string.Empty);

        // ✅ Commit session to ensure cookie is set
        await HttpContext.Session.CommitAsync();

        _logger.LogInformation($"✅ User logged in: {result.Id}");

        return Ok(new { member = result });
    }
    catch (AppException ex)
    {
        return StatusCode((int)ex.Code, new { success = false, message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error login");
        return StatusCode(500, new { success = false, message = "Something went wrong" });
    }
}
```

## Summary of Changes

| Component        | Change                                  | Why                             |
| ---------------- | --------------------------------------- | ------------------------------- |
| MemberService.ts | Add `validateSession()` method          | Verify session on app load      |
| MemberService.ts | Always use axios with `withCredentials` | Ensure cookies sent             |
| App.tsx          | Call `validateSession()` on mount       | Check if user still logged in   |
| Backend Login    | Add `await Session.CommitAsync()`       | Force session cookie to be sent |

## Testing Flow

```
1. Open Admin Dashboard
   → Login as admin
   → See admin dashboard ✅

2. Open React App (localhost:3000) in another tab
   → Login as user
   → See user dashboard ✅

3. Refresh Admin tab
   → Should still be logged in as admin ✅

4. Refresh React tab
   → Should still be logged in as user ✅

5. Logout from Admin
   → Admin redirects to login ✅
   → React tab still works (user still logged in) ✅

6. Logout from React
   → React redirects to login ✅
   → Admin tab still works (admin still logged in) ✅

7. Close React tab entirely
   → Logout from Admin
   → Admin redirects to login ✅
```

## Key Points

✅ **Session Keys are isolated**: Admin*\* and User*\* don't conflict  
✅ **Same session cookie**: Both apps use `.AspNetCore.Session`  
✅ **Independent logouts**: Logout from one doesn't affect the other  
✅ **Refresh persistence**: Each app validates its own session on page load  
✅ **localStorage sync**: Keeps frontend UI in sync with backend session

The key is that **each frontend app must validate the session independently** when it loads or when a request fails with 401.
