---
name: Cyber-Shield Enterprise
colors:
  surface: '#0c1324'
  surface-dim: '#0c1324'
  surface-bright: '#33394c'
  surface-container-lowest: '#070d1f'
  surface-container-low: '#151b2d'
  surface-container: '#191f31'
  surface-container-high: '#23293c'
  surface-container-highest: '#2e3447'
  on-surface: '#dce1fb'
  on-surface-variant: '#c1c6d7'
  inverse-surface: '#dce1fb'
  inverse-on-surface: '#2a3043'
  outline: '#8b91a0'
  outline-variant: '#414754'
  surface-tint: '#abc7ff'
  primary: '#abc7ff'
  on-primary: '#002f66'
  primary-container: '#448fff'
  on-primary-container: '#002859'
  inverse-primary: '#005cbc'
  secondary: '#b9c8de'
  on-secondary: '#233143'
  secondary-container: '#39485a'
  on-secondary-container: '#a7b6cc'
  tertiary: '#bec6e0'
  on-tertiary: '#283044'
  tertiary-container: '#8990a8'
  on-tertiary-container: '#22293d'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#d7e2ff'
  primary-fixed-dim: '#abc7ff'
  on-primary-fixed: '#001b3f'
  on-primary-fixed-variant: '#004590'
  secondary-fixed: '#d4e4fa'
  secondary-fixed-dim: '#b9c8de'
  on-secondary-fixed: '#0d1c2d'
  on-secondary-fixed-variant: '#39485a'
  tertiary-fixed: '#dae2fd'
  tertiary-fixed-dim: '#bec6e0'
  on-tertiary-fixed: '#131b2e'
  on-tertiary-fixed-variant: '#3f465c'
  background: '#0c1324'
  on-background: '#dce1fb'
  surface-variant: '#2e3447'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '700'
    lineHeight: 56px
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: 40px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: 28px
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  label-md:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.05em
  mono-md:
    fontFamily: JetBrains Mono
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  unit: 4px
  gutter: 24px
  margin: 32px
  container-max: 1440px
---

## Brand & Style

The visual identity of this design system is rooted in **Corporate Modernism** with a high-tech, security-focused edge. It is engineered for a licensing software suite where trust, precision, and robustness are paramount. The aesthetic balances the cold, structural integrity of silver and black with the energetic, "active" feel of deep blues. 

The personality is authoritative and technical. To achieve this, the design system employs a dark-mode-first approach to reduce eye strain for power users and to create a premium, "operation center" atmosphere. Visual motifs include sharp, well-defined borders and subtle inner glows that suggest a machine-tooled finish and active digital monitoring.

## Colors

The palette is derived from high-strength metals and digital light.
- **Primary (Electric Blue):** Used for primary actions, active indicators, and critical "success" states. It should be applied sparingly to maintain high impact.
- **Secondary (Silver/Slate):** Used for borders, icons, and secondary text. This color provides the structural "metallic" feel of the UI.
- **Backgrounds (Deep Navy/Black):** A tiered system of dark neutrals. The base is a near-black, while surfaces and containers use a deep navy to provide depth.
- **Functional Colors:** Clear, high-contrast green and red for validation. These should be accompanied by the same "glow" effect used for primary elements to ensure they feel part of the high-tech ecosystem.

## Typography

This design system utilizes **Inter** for all standard UI elements due to its exceptional legibility and modern, neutral character. For licensing keys, code snippets, and technical data, **JetBrains Mono** is introduced to reinforce the high-tech, developer-friendly nature of the suite.

Typography is strictly hierarchical. Labels use uppercase styling with increased letter spacing to emulate industrial markings. Headline weights remain semi-bold or bold to project authority and structure.

## Layout & Spacing

The layout is a **fixed-width, 12-column grid** optimized for professional desktop environments. It prioritizes information density and structured data entry.

- **Desktop (1440px+):** 12 columns, 24px gutters, 32px margins. 
- **Forms:** Large-scale forms are split into two-column layouts or centered single-column "focus" cards to prevent horizontal eye fatigue.
- **Data Tables:** Horizontal density is high, utilizing 8px vertical padding to maximize the number of visible rows.
- **Rhythm:** All spacing (padding, margin, gaps) follows a 4px base unit to ensure pixel-perfect alignment and a "tight," engineered feel.

## Elevation & Depth

Depth is created through **Tonal Layers** rather than traditional shadows. This maintains a clean, professional profile.

- **Base Layer:** The darkest shade, used for the main application background.
- **Surface Layer:** A slightly lighter navy blue used for cards, panels, and sidebars. Surfaces use a 1px solid border in a low-opacity silver (#94A3B8, 20%).
- **Active State (The "Glow"):** Elements that are focused or active (e.g., a selected license or an active text input) use a subtle **Outer Glow**. This is a drop shadow with 0px offset, 8px blur, and the primary blue color at 30% opacity. This mimics a backlit hardware interface.

## Shapes

To maintain a "secure and robust" feel, this design system uses **Soft (0.25rem)** roundedness. This provides enough softening to feel modern without losing the geometric, structural integrity of a technical tool. 

Interactive elements like buttons and inputs use the standard 4px radius. Larger containers (cards, modals) may scale up to 8px (rounded-lg) to create a visual distinction between the frame and the content.

## Components

### Buttons
Primary buttons use a solid primary blue background with white text. On hover, they gain the "active glow" effect. Secondary buttons use a silver border with transparent backgrounds. Tertiary buttons are text-only with icons, used for low-priority actions.

### Input Fields
Inputs are dark-themed with a subtle 1px border. When focused, the border transitions to primary blue and triggers the signature glow. Error states replace the blue border/glow with red, accompanied by a small alert icon.

### Data Tables & Lists
Lists are high-density. Each row is separated by a 1px subtle divider. "Active" or "Selected" rows are indicated by a 2px vertical accent bar on the left edge in primary blue.

### Chips & Badges
Used for license status (e.g., "Active", "Expired"). These use a "subtle fill" approach: a low-opacity background color with a high-contrast border and text in the same hue.

### Key/License Displays
License keys should always be rendered in **JetBrains Mono** within a recessed, dark-gray container to differentiate them from standard UI text and suggest they are "stored" data.

### Progress & Validation
For long-running licensing operations, use a linear progress bar with a subtle pulse effect in primary blue. Success feedback should be immediate, using a checkmark icon and a brief "Success" glow on the relevant card.